using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;
using CarRental.Domain.Policies;

namespace CarRental.Application.Services;

public class RentalRequestService : IRentalRequestService
{
    private readonly IUserRepository _users;
    private readonly ICarRepository _cars;
    private readonly IRentalRequestRepository _requests;
    private readonly IClock _clock;

    public RentalRequestService(
        IUserRepository users,
        ICarRepository cars,
        IRentalRequestRepository requests,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(cars);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(clock);
        _users = users;
        _cars = cars;
        _requests = requests;
        _clock = clock;
    }

    public async Task<RentalRequest> CreateAsync(
        int clientId,
        int carId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default)
    {
        if (end <= start)
        {
            throw new InvalidRentalRequestException("Rental end date must be strictly after start date.");
        }

        var client = await _users.GetByIdAsync(clientId, cancellationToken)
            ?? throw new UserNotFoundException(clientId);
        if (!client.HasRole(UserRole.Client))
        {
            throw new UnauthorizedRoleException(
                $"User {clientId} is not a client and cannot create rental requests.");
        }

        var car = await _cars.GetByIdAsync(carId, cancellationToken)
            ?? throw new CarNotFoundException(carId);

        // Eligibility (age / experience / powerful-car rules).
        var reason = ClientEligibilityPolicy.Check(client, car);
        if (reason is not null)
        {
            throw new ClientNotEligibleException(reason);
        }

        // Car status check: only Available cars can be rented.
        if (car.Status != CarStatus.Available)
        {
            throw new CarNotAvailableException(car.Id, $"current status is {car.Status}.");
        }

        // Calendar overlap with another approved/active rental.
        if (await _requests.HasOverlapAsync(car.Id, start, end, cancellationToken))
        {
            throw new CarNotAvailableException(car.Id, "requested dates overlap an existing approved rental.");
        }

        var request = new RentalRequest
        {
            ClientId = client.Id,
            CarId = car.Id,
            StartDate = start,
            EndDate = end,
            Status = RentalRequestStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        // Auto-approval: client who is also a manager confirms their own contract.
        if (client.HasRole(UserRole.Manager))
        {
            await ApplyApprovalAsync(request, car, resolverId: client.Id, cancellationToken);
        }

        await _requests.AddAsync(request, cancellationToken);
        return request;
    }

    public async Task ApproveAsync(int requestId, int managerId, CancellationToken cancellationToken = default)
    {
        var (request, manager) = await LoadForResolutionAsync(requestId, managerId, cancellationToken);
        var car = await _cars.GetByIdAsync(request.CarId, cancellationToken)
            ?? throw new CarNotFoundException(request.CarId);

        // Re-validate at approval time: car status or calendar may have changed.
        if (car.Status != CarStatus.Available)
        {
            throw new CarNotAvailableException(car.Id, $"current status is {car.Status}.");
        }
        if (await _requests.HasOverlapAsync(car.Id, request.StartDate, request.EndDate, cancellationToken))
        {
            throw new CarNotAvailableException(car.Id, "requested dates overlap an existing approved rental.");
        }

        await ApplyApprovalAsync(request, car, resolverId: manager.Id, cancellationToken);
        await _requests.UpdateAsync(request, cancellationToken);
    }

    public async Task RejectAsync(int requestId, int managerId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason must not be empty.", nameof(reason));
        }

        var (request, manager) = await LoadForResolutionAsync(requestId, managerId, cancellationToken);

        request.Status = RentalRequestStatus.Rejected;
        request.ResolverId = manager.Id;
        request.ResolvedAt = _clock.UtcNow;
        request.RejectionReason = reason;

        await _requests.UpdateAsync(request, cancellationToken);
    }

    public async Task CompleteAsync(
        int requestId,
        int managerId,
        DateOnly actualReturnDate,
        bool damaged,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new RentalRequestNotFoundException(requestId);
        if (request.Status != RentalRequestStatus.Approved)
        {
            throw new RentalRequestNotApprovedException(requestId);
        }

        var manager = await _users.GetByIdAsync(managerId, cancellationToken)
            ?? throw new UserNotFoundException(managerId);
        if (!manager.HasRole(UserRole.Manager))
        {
            throw new UnauthorizedRoleException(
                $"User {managerId} is not a manager and cannot complete rentals.");
        }

        var car = await _cars.GetByIdAsync(request.CarId, cancellationToken)
            ?? throw new CarNotFoundException(request.CarId);

        if (actualReturnDate < request.StartDate)
        {
            throw new InvalidRentalRequestException(
                "Actual return date cannot be earlier than the rental start date.");
        }

        int lateDays = Math.Max(0, actualReturnDate.DayNumber - request.EndDate.DayNumber);
        decimal basePrice = request.Price ?? RentalPricing.CalculateBase(car.DailyTariff, request.DurationDays);
        decimal penalty = RentalPricing.CalculatePenalty(car.DailyTariff, basePrice, lateDays, damaged);

        request.Status = RentalRequestStatus.Completed;
        request.Penalty = penalty;
        request.ActualReturnDate = actualReturnDate;
        request.Damaged = damaged;
        request.ResolverId = manager.Id;
        request.ResolvedAt = _clock.UtcNow;
        request.Price = basePrice;

        // Free the car for the next renter, unless the manager has put it into
        // maintenance in the meantime (e.g. severe damage).
        if (car.Status == CarStatus.Rented)
        {
            car.Status = CarStatus.Available;
            await _cars.UpdateAsync(car, cancellationToken);
        }

        await _requests.UpdateAsync(request, cancellationToken);
    }

    private async Task<(RentalRequest Request, User Manager)> LoadForResolutionAsync(
        int requestId,
        int managerId,
        CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new RentalRequestNotFoundException(requestId);
        if (request.Status != RentalRequestStatus.Pending)
        {
            throw new RentalRequestAlreadyResolvedException(requestId);
        }
        var manager = await _users.GetByIdAsync(managerId, cancellationToken)
            ?? throw new UserNotFoundException(managerId);
        if (!manager.HasRole(UserRole.Manager))
        {
            throw new UnauthorizedRoleException(
                $"User {managerId} is not a manager and cannot resolve rental requests.");
        }
        return (request, manager);
    }

    private async Task ApplyApprovalAsync(
        RentalRequest request,
        Car car,
        int resolverId,
        CancellationToken cancellationToken)
    {
        request.Status = RentalRequestStatus.Approved;
        request.ResolverId = resolverId;
        request.ResolvedAt = _clock.UtcNow;
        request.Price = RentalPricing.CalculateBase(car.DailyTariff, request.DurationDays);

        car.Status = CarStatus.Rented;
        await _cars.UpdateAsync(car, cancellationToken);
    }
}

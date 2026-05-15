using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IRentalRequestService
{
    Task<RentalRequest> CreateAsync(
        int clientId,
        int carId,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(int requestId, int managerId, CancellationToken cancellationToken = default);

    Task RejectAsync(int requestId, int managerId, string reason, CancellationToken cancellationToken = default);

    // Closes an Approved request: marks it Completed, records actual return date
    // and damage flag, computes the final penalty and frees the car (Rented -> Available).
    Task CompleteAsync(
        int requestId,
        int managerId,
        DateOnly actualReturnDate,
        bool damaged,
        CancellationToken cancellationToken = default);
}

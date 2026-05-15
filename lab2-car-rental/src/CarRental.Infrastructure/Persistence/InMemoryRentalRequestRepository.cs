using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Interfaces;

namespace CarRental.Infrastructure.Persistence;

// In-memory implementation. EF Core context would replace this in a future iteration.
public class InMemoryRentalRequestRepository : IRentalRequestRepository
{
    private readonly Dictionary<int, RentalRequest> _byId = new();
    private int _nextId = 1;

    public Task<RentalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var request);
        return Task.FromResult(request);
    }

    public Task AddAsync(RentalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == 0)
        {
            request.Id = _nextId++;
        }
        else if (request.Id >= _nextId)
        {
            _nextId = request.Id + 1;
        }
        _byId[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RentalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_byId.ContainsKey(request.Id))
        {
            throw new KeyNotFoundException($"Rental request {request.Id} was not found.");
        }
        _byId[request.Id] = request;
        return Task.CompletedTask;
    }

    // Two half-open intervals [s1, e1) and [s2, e2) overlap iff s1 < e2 AND s2 < e1.
    // Only Approved rentals block the calendar — Pending requests are not yet
    // confirmed contracts, Rejected/Completed rentals do not hold the car.
    public Task<bool> HasOverlapAsync(int carId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        foreach (var request in _byId.Values)
        {
            if (request.CarId != carId)
            {
                continue;
            }
            if (request.Status != RentalRequestStatus.Approved)
            {
                continue;
            }
            if (start < request.EndDate && request.StartDate < end)
            {
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }
}

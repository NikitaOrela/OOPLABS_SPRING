using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IRentalRequestService
{
    Task<RentalRequest> CreateAsync(int clientId, int carId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task ApproveAsync(int requestId, int managerId, CancellationToken cancellationToken = default);
    Task RejectAsync(int requestId, int managerId, CancellationToken cancellationToken = default);
    Task CompleteAsync(int requestId, int managerId, decimal? penalty, CancellationToken cancellationToken = default);
}

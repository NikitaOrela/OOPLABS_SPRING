using CarRental.Domain.Entities;

namespace CarRental.Domain.Interfaces;

public interface IRentalRequestRepository
{
    Task<RentalRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(RentalRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(RentalRequest request, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapAsync(int carId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}

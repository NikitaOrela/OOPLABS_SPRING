using CarRental.Domain.Entities;

namespace CarRental.Domain.Interfaces;

public interface ICarRepository
{
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken = default);
    Task AddAsync(Car car, CancellationToken cancellationToken = default);
    Task UpdateAsync(Car car, CancellationToken cancellationToken = default);
}

using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions;

public interface ICarService
{
    Task<Car> CreateAsync(
        int managerId,
        string vin,
        string make,
        string model,
        int powerHp,
        decimal dailyTariff,
        CancellationToken cancellationToken = default);

    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Car> UpdateStatusAsync(
        int carId,
        int managerId,
        CarStatus newStatus,
        CancellationToken cancellationToken = default);
}

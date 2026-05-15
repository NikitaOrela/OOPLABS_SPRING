using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;

namespace CarRental.Application.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _cars;
    private readonly IUserRepository _users;

    public CarService(ICarRepository cars, IUserRepository users)
    {
        ArgumentNullException.ThrowIfNull(cars);
        ArgumentNullException.ThrowIfNull(users);
        _cars = cars;
        _users = users;
    }

    public async Task<Car> CreateAsync(
        int managerId,
        string vin,
        string make,
        string model,
        int powerHp,
        decimal dailyTariff,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vin))
        {
            throw new ArgumentException("VIN must not be empty.", nameof(vin));
        }
        if (string.IsNullOrWhiteSpace(make))
        {
            throw new ArgumentException("Make must not be empty.", nameof(make));
        }
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model must not be empty.", nameof(model));
        }
        if (powerHp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(powerHp), "Power must be positive.");
        }
        if (dailyTariff <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyTariff), "Daily tariff must be positive.");
        }

        var manager = await _users.GetByIdAsync(managerId, cancellationToken)
            ?? throw new UserNotFoundException(managerId);
        if (!manager.HasRole(UserRole.Manager))
        {
            throw new UnauthorizedRoleException(
                $"User {managerId} is not a manager and cannot register cars.");
        }

        if (await _cars.GetByVinAsync(vin, cancellationToken) is not null)
        {
            throw new DuplicateVinException(vin);
        }

        var car = new Car
        {
            Vin = vin,
            Make = make,
            Model = model,
            PowerHp = powerHp,
            DailyTariff = dailyTariff,
            Status = CarStatus.Available
        };
        await _cars.AddAsync(car, cancellationToken);
        return car;
    }

    public Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _cars.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Car> UpdateStatusAsync(
        int carId,
        int managerId,
        CarStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(CarStatus), newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus), "Unknown car status.");
        }

        var manager = await _users.GetByIdAsync(managerId, cancellationToken)
            ?? throw new UserNotFoundException(managerId);
        if (!manager.HasRole(UserRole.Manager))
        {
            throw new UnauthorizedRoleException(
                $"User {managerId} is not a manager and cannot change car status.");
        }

        var car = await _cars.GetByIdAsync(carId, cancellationToken)
            ?? throw new CarNotFoundException(carId);

        car.Status = newStatus;
        await _cars.UpdateAsync(car, cancellationToken);
        return car;
    }
}

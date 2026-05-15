using CarRental.Domain.Entities;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;

namespace CarRental.Infrastructure.Persistence;

// Placeholder in-memory implementation. EF Core context will replace this in Lab 2.
public class InMemoryCarRepository : ICarRepository
{
    private readonly Dictionary<int, Car> _byId = new();
    private readonly Dictionary<string, int> _idByVin = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;

    public Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var car);
        return Task.FromResult(car);
    }

    public Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken = default)
    {
        if (_idByVin.TryGetValue(vin, out var id))
        {
            return GetByIdAsync(id, cancellationToken);
        }
        return Task.FromResult<Car?>(null);
    }

    public Task AddAsync(Car car, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (_idByVin.ContainsKey(car.Vin))
        {
            throw new DuplicateVinException(car.Vin);
        }
        car.Id = _nextId++;
        _byId[car.Id] = car;
        _idByVin[car.Vin] = car.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(car);
        _byId[car.Id] = car;
        return Task.CompletedTask;
    }
}

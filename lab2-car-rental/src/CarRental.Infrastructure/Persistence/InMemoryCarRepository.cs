using CarRental.Domain.Entities;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;

namespace CarRental.Infrastructure.Persistence;

// In-memory implementation. Holds state in process memory; everything is lost
// on restart. Acceptable for the educational scope of Lab 2; EF Core would
// replace this in a later iteration.
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
        if (vin is not null && _idByVin.TryGetValue(vin, out var id))
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
        if (car.Id == 0)
        {
            car.Id = _nextId++;
        }
        else if (car.Id >= _nextId)
        {
            _nextId = car.Id + 1;
        }
        _byId[car.Id] = car;
        _idByVin[car.Vin] = car.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (!_byId.ContainsKey(car.Id))
        {
            throw new KeyNotFoundException($"Car {car.Id} was not found.");
        }
        _byId[car.Id] = car;
        return Task.CompletedTask;
    }
}

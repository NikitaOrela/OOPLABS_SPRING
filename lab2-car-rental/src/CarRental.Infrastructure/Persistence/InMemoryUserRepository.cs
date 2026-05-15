using CarRental.Domain.Entities;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;

namespace CarRental.Infrastructure.Persistence;

// In-memory implementation. State lives only for the process lifetime.
public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<int, User> _byId = new();
    private readonly Dictionary<string, int> _idByUserName = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (userName is not null && _idByUserName.TryGetValue(userName, out var id))
        {
            return GetByIdAsync(id, cancellationToken);
        }
        return Task.FromResult<User?>(null);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (_idByUserName.ContainsKey(user.UserName))
        {
            throw new DuplicateUserNameException(user.UserName);
        }
        if (user.Id == 0)
        {
            user.Id = _nextId++;
        }
        else if (user.Id >= _nextId)
        {
            _nextId = user.Id + 1;
        }
        _byId[user.Id] = user;
        _idByUserName[user.UserName] = user.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!_byId.ContainsKey(user.Id))
        {
            throw new KeyNotFoundException($"User {user.Id} was not found.");
        }
        _byId[user.Id] = user;
        return Task.CompletedTask;
    }
}

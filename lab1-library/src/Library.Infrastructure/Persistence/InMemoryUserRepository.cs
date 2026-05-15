using Library.Domain.Entities;
using Library.Domain.Exceptions;
using Library.Domain.Interfaces;

namespace Library.Infrastructure.Persistence;

// Placeholder in-memory implementation. EF Core context will replace this in Lab 1.
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
        if (_idByUserName.TryGetValue(userName, out var id))
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
        user.Id = _nextId++;
        _byId[user.Id] = user;
        _idByUserName[user.UserName] = user.Id;
        return Task.CompletedTask;
    }
}

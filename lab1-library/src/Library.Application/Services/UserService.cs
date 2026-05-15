using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Domain.Interfaces;

namespace Library.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        ArgumentNullException.ThrowIfNull(users);
        _users = users;
    }

    public async Task<User> CreateAsync(
        string userName,
        string fullName,
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("User name must not be empty.", nameof(userName));
        }
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name must not be empty.", nameof(fullName));
        }
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0)
        {
            throw new InvalidRequestException("A user must have at least one role.");
        }
        foreach (var role in roles)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
            {
                throw new ArgumentOutOfRangeException(nameof(roles), $"Unknown role: {role}.");
            }
        }

        if (await _users.GetByUserNameAsync(userName, cancellationToken) is not null)
        {
            throw new DuplicateUserNameException(userName);
        }

        var user = new User
        {
            UserName = userName,
            FullName = fullName
        };
        foreach (var role in roles)
        {
            if (!user.Roles.Contains(role))
            {
                user.Roles.Add(role);
            }
        }

        await _users.AddAsync(user, cancellationToken);
        return user;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _users.GetByIdAsync(id, cancellationToken);
    }
}

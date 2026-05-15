using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Domain.Interfaces;

namespace CarRental.Application.Services;

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
        int age,
        int drivingExperienceYears,
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
        if (age <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(age), "Age must be positive.");
        }
        if (drivingExperienceYears < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(drivingExperienceYears), "Driving experience must be non-negative.");
        }
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0)
        {
            throw new InvalidRentalRequestException("A user must have at least one role.");
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
            FullName = fullName,
            Age = age,
            DrivingExperienceYears = drivingExperienceYears
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

    public async Task<User> UpdateRolesAsync(
        int targetUserId,
        int administratorId,
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0)
        {
            throw new InvalidRentalRequestException("A user must have at least one role.");
        }
        foreach (var role in roles)
        {
            if (!Enum.IsDefined(typeof(UserRole), role))
            {
                throw new ArgumentOutOfRangeException(nameof(roles), $"Unknown role: {role}.");
            }
        }

        var admin = await _users.GetByIdAsync(administratorId, cancellationToken)
            ?? throw new UserNotFoundException(administratorId);
        if (!admin.HasRole(UserRole.Administrator))
        {
            throw new UnauthorizedRoleException(
                $"User {administratorId} is not an administrator and cannot manage roles.");
        }

        var target = await _users.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new UserNotFoundException(targetUserId);

        target.Roles.Clear();
        foreach (var role in roles)
        {
            if (!target.Roles.Contains(role))
            {
                target.Roles.Add(role);
            }
        }
        await _users.UpdateAsync(target, cancellationToken);
        return target;
    }
}

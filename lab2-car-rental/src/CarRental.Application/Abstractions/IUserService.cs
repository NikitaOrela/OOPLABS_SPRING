using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions;

public interface IUserService
{
    Task<User> CreateAsync(
        string userName,
        string fullName,
        int age,
        int drivingExperienceYears,
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Administrator-only: replace the role set of an existing user.
    Task<User> UpdateRolesAsync(
        int targetUserId,
        int administratorId,
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default);
}

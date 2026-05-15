using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Abstractions;

public interface IUserService
{
    Task<User> CreateAsync(string userName, string fullName, IReadOnlyCollection<UserRole> roles, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Infrastructure.Persistence;
using Xunit;

namespace CarRental.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_HappyPath_AssignsRoles()
    {
        var users = new InMemoryUserRepository();
        var service = new UserService(users);

        var created = await service.CreateAsync(
            "alice", "Alice A.", 30, 5,
            new[] { UserRole.Client });

        Assert.True(created.Id > 0);
        Assert.Contains(UserRole.Client, created.Roles);
    }

    [Fact]
    public async Task CreateUser_DuplicateUserName_Throws()
    {
        var users = new InMemoryUserRepository();
        var service = new UserService(users);

        await service.CreateAsync("alice", "Alice", 30, 5, new[] { UserRole.Client });
        await Assert.ThrowsAsync<DuplicateUserNameException>(() =>
            service.CreateAsync("alice", "Other", 40, 10, new[] { UserRole.Manager }));
    }

    [Fact]
    public async Task CreateUser_EmptyRoles_Throws()
    {
        var users = new InMemoryUserRepository();
        var service = new UserService(users);

        await Assert.ThrowsAsync<InvalidRentalRequestException>(() =>
            service.CreateAsync("alice", "Alice", 30, 5, Array.Empty<UserRole>()));
    }

    [Fact]
    public async Task UpdateRoles_ByAdministrator_ReplacesRoles()
    {
        var users = new InMemoryUserRepository();
        var service = new UserService(users);

        var admin = await users.GetByUserNameAsync("admin");
        Assert.Null(admin);

        var adminUser = await service.CreateAsync("admin", "Root", 40, 10, new[] { UserRole.Administrator });
        var alice = await service.CreateAsync("alice", "Alice", 30, 5, new[] { UserRole.Client });

        var updated = await service.UpdateRolesAsync(alice.Id, adminUser.Id, new[] { UserRole.Manager });

        Assert.Single(updated.Roles);
        Assert.Contains(UserRole.Manager, updated.Roles);
        Assert.DoesNotContain(UserRole.Client, updated.Roles);
    }

    [Fact]
    public async Task UpdateRoles_ByNonAdministrator_Throws()
    {
        var users = new InMemoryUserRepository();
        var service = new UserService(users);

        var manager = await service.CreateAsync("mgr", "Manager", 40, 10, new[] { UserRole.Manager });
        var alice = await service.CreateAsync("alice", "Alice", 30, 5, new[] { UserRole.Client });

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            service.UpdateRolesAsync(alice.Id, manager.Id, new[] { UserRole.Manager }));
    }
}

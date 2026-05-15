using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Infrastructure.Persistence;
using Xunit;

namespace CarRental.Tests;

public class CarServiceTests
{
    private static async Task<User> AddUserAsync(
        InMemoryUserRepository users,
        string name,
        params UserRole[] roles)
    {
        var user = new User { UserName = name, FullName = name, Age = 30, DrivingExperienceYears = 5 };
        foreach (var role in roles)
        {
            user.Roles.Add(role);
        }
        await users.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task CreateCar_ByManager_RegistersCar()
    {
        var users = new InMemoryUserRepository();
        var cars = new InMemoryCarRepository();
        var manager = await AddUserAsync(users, "mgr", UserRole.Manager);
        var service = new CarService(cars, users);

        var car = await service.CreateAsync(manager.Id, "VIN001", "Make", "Model", 120, 50m);

        Assert.True(car.Id > 0);
        Assert.Equal("VIN001", car.Vin);
        Assert.Equal(CarStatus.Available, car.Status);
    }

    [Fact]
    public async Task CreateCar_ByNonManager_Throws()
    {
        var users = new InMemoryUserRepository();
        var cars = new InMemoryCarRepository();
        var client = await AddUserAsync(users, "alice", UserRole.Client);
        var service = new CarService(cars, users);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            service.CreateAsync(client.Id, "VIN001", "Make", "Model", 120, 50m));
    }

    [Fact]
    public async Task CreateCar_DuplicateVin_Throws()
    {
        var users = new InMemoryUserRepository();
        var cars = new InMemoryCarRepository();
        var manager = await AddUserAsync(users, "mgr", UserRole.Manager);
        var service = new CarService(cars, users);

        await service.CreateAsync(manager.Id, "VIN001", "Make", "Model", 120, 50m);
        await Assert.ThrowsAsync<DuplicateVinException>(() =>
            service.CreateAsync(manager.Id, "VIN001", "Other", "Other", 200, 80m));
    }

    [Fact]
    public async Task UpdateStatus_ByManager_ChangesStatus()
    {
        var users = new InMemoryUserRepository();
        var cars = new InMemoryCarRepository();
        var manager = await AddUserAsync(users, "mgr", UserRole.Manager);
        var service = new CarService(cars, users);

        var car = await service.CreateAsync(manager.Id, "VIN001", "Make", "Model", 120, 50m);
        var updated = await service.UpdateStatusAsync(car.Id, manager.Id, CarStatus.UnderMaintenance);

        Assert.Equal(CarStatus.UnderMaintenance, updated.Status);
    }

    [Fact]
    public async Task UpdateStatus_ByNonManager_Throws()
    {
        var users = new InMemoryUserRepository();
        var cars = new InMemoryCarRepository();
        var manager = await AddUserAsync(users, "mgr", UserRole.Manager);
        var client = await AddUserAsync(users, "alice", UserRole.Client);
        var service = new CarService(cars, users);

        var car = await service.CreateAsync(manager.Id, "VIN001", "Make", "Model", 120, 50m);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            service.UpdateStatusAsync(car.Id, client.Id, CarStatus.UnderMaintenance));
    }
}

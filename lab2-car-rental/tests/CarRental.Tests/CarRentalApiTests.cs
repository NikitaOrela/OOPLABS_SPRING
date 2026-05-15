using System.Net;
using System.Net.Http.Json;
using CarRental.Domain.Enums;
using CarRental.Presentation;
using CarRental.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CarRental.Tests;

public class CarRentalApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CarRentalApiTests(WebApplicationFactory<Program> factory)
    {
        // Each test class instance gets a fresh host => isolated in-memory state.
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    private HttpClient NewClient() => _factory.CreateClient();

    private static DateOnly D(int day) => new DateOnly(2026, 7, day);

    [Fact]
    public async Task FullHappyPath_CreateUsersCar_RentApproveComplete()
    {
        var client = NewClient();

        var manager = await CreateUserAsync(client, "mgr1", "Manager One", 40, 10, UserRole.Manager);
        var renter = await CreateUserAsync(client, "client1", "Client One", 30, 5, UserRole.Client);

        var car = await CreateCarAsync(client, manager.Id, "VINHAPPY1", "Toyota", "Corolla", 120, 50m);
        Assert.Equal(CarStatus.Available, car.Status);

        var request = await CreateRentalAsync(client, renter.Id, car.Id, D(1), D(4));
        Assert.Equal(RentalRequestStatus.Pending, request.Status);

        var approved = await ApproveAsync(client, request.Id, manager.Id);
        Assert.Equal(RentalRequestStatus.Approved, approved.Status);
        Assert.Equal(150m, approved.Price);

        var carAfter = await GetCarAsync(client, car.Id);
        Assert.Equal(CarStatus.Rented, carAfter.Status);

        var completed = await CompleteAsync(client, request.Id, manager.Id, D(4), damaged: false);
        Assert.Equal(RentalRequestStatus.Completed, completed.Status);
        Assert.Equal(0m, completed.Penalty);

        var carDone = await GetCarAsync(client, car.Id);
        Assert.Equal(CarStatus.Available, carDone.Status);
    }

    [Fact]
    public async Task DuplicateUserName_Returns409()
    {
        var client = NewClient();
        await CreateUserAsync(client, "dup", "X", 30, 5, UserRole.Client);
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            UserName = "dup",
            FullName = "Y",
            Age = 31,
            DrivingExperienceYears = 5,
            Roles = new List<UserRole> { UserRole.Client }
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateVin_Returns409()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_dvin", "Mgr", 40, 10, UserRole.Manager);
        await CreateCarAsync(client, manager.Id, "VINDUP", "Make", "Model", 120, 50m);

        var response = await client.PostAsJsonAsync("/api/cars", new CreateCarRequest
        {
            ManagerId = manager.Id,
            Vin = "VINDUP",
            Make = "Other",
            Model = "Other",
            PowerHp = 200,
            DailyTariff = 80m
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CarRegistrationByNonManager_Returns403()
    {
        var client = NewClient();
        var clientUser = await CreateUserAsync(client, "alice_nm", "Alice", 30, 5, UserRole.Client);
        var response = await client.PostAsJsonAsync("/api/cars", new CreateCarRequest
        {
            ManagerId = clientUser.Id,
            Vin = "VINNM",
            Make = "Make",
            Model = "Model",
            PowerHp = 120,
            DailyTariff = 50m
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCarStatusByNonManager_Returns403()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_us", "Mgr", 40, 10, UserRole.Manager);
        var nonManager = await CreateUserAsync(client, "alice_us", "Alice", 30, 5, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINUS", "Make", "Model", 120, 50m);

        var response = await client.PostAsJsonAsync($"/api/cars/{car.Id}/status", new UpdateCarStatusRequest
        {
            ManagerId = nonManager.Id,
            Status = CarStatus.UnderMaintenance
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TooYoungClient_Returns422()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_young", "Mgr", 40, 10, UserRole.Manager);
        var youngster = await CreateUserAsync(client, "kid", "Kid", 19, 1, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINYOUNG", "Make", "Model", 120, 50m);

        var response = await client.PostAsJsonAsync("/api/rentals", new CreateRentalRequestRequest
        {
            ClientId = youngster.Id,
            CarId = car.Id,
            StartDate = D(1),
            EndDate = D(4)
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task NotEnoughExperience_Returns422()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_exp", "Mgr", 40, 10, UserRole.Manager);
        var fresh = await CreateUserAsync(client, "fresh", "Fresh", 25, 1, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINEXP", "Make", "Model", 120, 50m);

        var response = await client.PostAsJsonAsync("/api/rentals", new CreateRentalRequestRequest
        {
            ClientId = fresh.Id,
            CarId = car.Id,
            StartDate = D(1),
            EndDate = D(4)
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UnavailableCar_Returns422()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_unavail", "Mgr", 40, 10, UserRole.Manager);
        var renter = await CreateUserAsync(client, "renter_unavail", "Renter", 30, 5, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINUNAV", "Make", "Model", 120, 50m);

        // Manager flips it to UnderMaintenance.
        var statusResponse = await client.PostAsJsonAsync($"/api/cars/{car.Id}/status", new UpdateCarStatusRequest
        {
            ManagerId = manager.Id,
            Status = CarStatus.UnderMaintenance
        });
        statusResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/rentals", new CreateRentalRequestRequest
        {
            ClientId = renter.Id,
            CarId = car.Id,
            StartDate = D(1),
            EndDate = D(4)
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task OverlappingApprovedRental_Returns422()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_overlap", "Mgr", 40, 10, UserRole.Manager);
        var clientA = await CreateUserAsync(client, "alice_overlap", "Alice", 30, 5, UserRole.Client);
        var clientB = await CreateUserAsync(client, "bob_overlap", "Bob", 30, 5, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINOVR", "Make", "Model", 120, 50m);

        var first = await CreateRentalAsync(client, clientA.Id, car.Id, D(1), D(5));
        await ApproveAsync(client, first.Id, manager.Id);

        // Manager flips car back to Available manually to prove the date overlap blocks even then.
        var statusResponse = await client.PostAsJsonAsync($"/api/cars/{car.Id}/status", new UpdateCarStatusRequest
        {
            ManagerId = manager.Id,
            Status = CarStatus.Available
        });
        statusResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/rentals", new CreateRentalRequestRequest
        {
            ClientId = clientB.Id,
            CarId = car.Id,
            StartDate = D(3),
            EndDate = D(6)
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ApproveByNonManager_Returns403()
    {
        var client = NewClient();
        var manager = await CreateUserAsync(client, "mgr_ap", "Mgr", 40, 10, UserRole.Manager);
        var renter = await CreateUserAsync(client, "renter_ap", "Renter", 30, 5, UserRole.Client);
        var car = await CreateCarAsync(client, manager.Id, "VINAP", "Make", "Model", 120, 50m);
        var request = await CreateRentalAsync(client, renter.Id, car.Id, D(1), D(4));

        var response = await client.PostAsJsonAsync($"/api/rentals/{request.Id}/approve",
            new ApproveRentalRequestRequest { ManagerId = renter.Id });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClientManager_AutoApprovesRental()
    {
        var client = NewClient();
        var hybrid = await CreateUserAsync(client, "ceo", "CEO", 30, 5, UserRole.Client, UserRole.Manager);
        var manager = await CreateUserAsync(client, "mgr_ceo", "Mgr", 40, 10, UserRole.Manager);
        var car = await CreateCarAsync(client, manager.Id, "VINCEO", "Make", "Model", 120, 50m);

        var request = await CreateRentalAsync(client, hybrid.Id, car.Id, D(1), D(3));
        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal(hybrid.Id, request.ResolverId);
        Assert.Equal(100m, request.Price);
    }

    // ---- helpers ----------------------------------------------------------

    private static async Task<UserResponse> CreateUserAsync(
        HttpClient client,
        string userName,
        string fullName,
        int age,
        int experience,
        params UserRole[] roles)
    {
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            UserName = userName,
            FullName = fullName,
            Age = age,
            DrivingExperienceYears = experience,
            Roles = roles.ToList()
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private static async Task<CarResponse> CreateCarAsync(
        HttpClient client,
        int managerId,
        string vin,
        string make,
        string model,
        int powerHp,
        decimal dailyTariff)
    {
        var response = await client.PostAsJsonAsync("/api/cars", new CreateCarRequest
        {
            ManagerId = managerId,
            Vin = vin,
            Make = make,
            Model = model,
            PowerHp = powerHp,
            DailyTariff = dailyTariff
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CarResponse>())!;
    }

    private static async Task<CarResponse> GetCarAsync(HttpClient client, int id)
    {
        var response = await client.GetAsync($"/api/cars/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CarResponse>())!;
    }

    private static async Task<RentalRequestResponse> CreateRentalAsync(
        HttpClient client,
        int clientId,
        int carId,
        DateOnly start,
        DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/rentals", new CreateRentalRequestRequest
        {
            ClientId = clientId,
            CarId = carId,
            StartDate = start,
            EndDate = end
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RentalRequestResponse>())!;
    }

    private static async Task<RentalRequestResponse> ApproveAsync(HttpClient client, int requestId, int managerId)
    {
        var response = await client.PostAsJsonAsync($"/api/rentals/{requestId}/approve",
            new ApproveRentalRequestRequest { ManagerId = managerId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RentalRequestResponse>())!;
    }

    private static async Task<RentalRequestResponse> CompleteAsync(
        HttpClient client,
        int requestId,
        int managerId,
        DateOnly actualReturn,
        bool damaged)
    {
        var response = await client.PostAsJsonAsync($"/api/rentals/{requestId}/complete",
            new CompleteRentalRequestRequest
            {
                ManagerId = managerId,
                ActualReturnDate = actualReturn,
                Damaged = damaged
            });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RentalRequestResponse>())!;
    }
}

using CarRental.Application.Abstractions;
using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Infrastructure.Persistence;
using Xunit;

namespace CarRental.Tests;

public class RentalRequestServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class TestWorld
    {
        public InMemoryUserRepository Users { get; } = new();
        public InMemoryCarRepository Cars { get; } = new();
        public InMemoryRentalRequestRepository Requests { get; } = new();
        public FixedClock Clock { get; } = new();
        public RentalRequestService Service { get; }

        public TestWorld()
        {
            Service = new RentalRequestService(Users, Cars, Requests, Clock);
        }

        public async Task<User> AddUserAsync(
            string name,
            int age = 30,
            int experience = 5,
            params UserRole[] roles)
        {
            var user = new User
            {
                UserName = name,
                FullName = name,
                Age = age,
                DrivingExperienceYears = experience
            };
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
            await Users.AddAsync(user);
            return user;
        }

        public async Task<Car> AddCarAsync(
            string vin = "VIN1",
            int powerHp = 150,
            decimal dailyTariff = 50m,
            CarStatus status = CarStatus.Available)
        {
            var car = new Car
            {
                Vin = vin,
                Make = "Make",
                Model = "Model",
                PowerHp = powerHp,
                DailyTariff = dailyTariff,
                Status = status
            };
            await Cars.AddAsync(car);
            return car;
        }
    }

    private static DateOnly D(int day) => new DateOnly(2026, 6, day);

    // ----- Create: happy path & validation --------------------------------

    [Fact]
    public async Task CreateRequest_HappyPath_IsPending()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", age: 30, experience: 5, UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));

        Assert.Equal(RentalRequestStatus.Pending, request.Status);
        Assert.Equal(client.Id, request.ClientId);
        Assert.Equal(car.Id, request.CarId);
        Assert.Equal(3, request.DurationDays);
        Assert.Null(request.Price);
        // Car must not be flipped to Rented before approval.
        Assert.Equal(CarStatus.Available, (await world.Cars.GetByIdAsync(car.Id))!.Status);
    }

    [Fact]
    public async Task CreateRequest_AsNonClient_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var car = await world.AddCarAsync();

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.CreateAsync(manager.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_EndBeforeStart_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        await Assert.ThrowsAsync<InvalidRentalRequestException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(4), D(1)));
    }

    // ----- Eligibility rules ----------------------------------------------

    [Fact]
    public async Task CreateRequest_TooYoungClient_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", age: 21, experience: 5, UserRole.Client);
        var car = await world.AddCarAsync();

        await Assert.ThrowsAsync<ClientNotEligibleException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_NotEnoughExperience_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", age: 30, experience: 1, UserRole.Client);
        var car = await world.AddCarAsync();

        await Assert.ThrowsAsync<ClientNotEligibleException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_PowerfulCar_TooYoung_Throws()
    {
        var world = new TestWorld();
        // 24-year-old, 5y experience: passes base rule, fails powerful-car rule (age >= 25).
        var client = await world.AddUserAsync("alice", age: 24, experience: 5, UserRole.Client);
        var car = await world.AddCarAsync(powerHp: 300);

        await Assert.ThrowsAsync<ClientNotEligibleException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_PowerfulCar_NotEnoughExperience_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", age: 30, experience: 3, UserRole.Client);
        var car = await world.AddCarAsync(powerHp: 300);

        await Assert.ThrowsAsync<ClientNotEligibleException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_PowerfulCar_MeetsStricterRule_IsPending()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", age: 25, experience: 5, UserRole.Client);
        var car = await world.AddCarAsync(powerHp: 300);

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        Assert.Equal(RentalRequestStatus.Pending, request.Status);
    }

    // ----- Car availability ------------------------------------------------

    [Fact]
    public async Task CreateRequest_CarUnderMaintenance_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(status: CarStatus.UnderMaintenance);

        await Assert.ThrowsAsync<CarNotAvailableException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_CarAlreadyRented_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(status: CarStatus.Rented);

        await Assert.ThrowsAsync<CarNotAvailableException>(() =>
            world.Service.CreateAsync(client.Id, car.Id, D(1), D(4)));
    }

    [Fact]
    public async Task CreateRequest_OverlapsExistingApprovedRental_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var clientA = await world.AddUserAsync("alice", roles: UserRole.Client);
        var clientB = await world.AddUserAsync("bob", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var first = await world.Service.CreateAsync(clientA.Id, car.Id, D(1), D(5));
        await world.Service.ApproveAsync(first.Id, manager.Id);

        // After approval, car is Rented; even after we return it to Available,
        // the date overlap with the approved rental must keep blocking.
        var carNow = await world.Cars.GetByIdAsync(car.Id);
        carNow!.Status = CarStatus.Available;
        await world.Cars.UpdateAsync(carNow);

        await Assert.ThrowsAsync<CarNotAvailableException>(() =>
            world.Service.CreateAsync(clientB.Id, car.Id, D(3), D(6)));
    }

    [Fact]
    public async Task CreateRequest_AdjacentInterval_IsAllowed()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var clientA = await world.AddUserAsync("alice", roles: UserRole.Client);
        var clientB = await world.AddUserAsync("bob", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var first = await world.Service.CreateAsync(clientA.Id, car.Id, D(1), D(5));
        await world.Service.ApproveAsync(first.Id, manager.Id);

        // Return the car so its status is back to Available before the next try.
        var carNow = await world.Cars.GetByIdAsync(car.Id);
        carNow!.Status = CarStatus.Available;
        await world.Cars.UpdateAsync(carNow);

        var second = await world.Service.CreateAsync(clientB.Id, car.Id, D(5), D(7));
        Assert.Equal(RentalRequestStatus.Pending, second.Status);
    }

    // ----- Approve ---------------------------------------------------------

    [Fact]
    public async Task ApproveRequest_HappyPath_RentsCarAndSetsPrice()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(dailyTariff: 40m);

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);

        var stored = (await world.Requests.GetByIdAsync(request.Id))!;
        Assert.Equal(RentalRequestStatus.Approved, stored.Status);
        Assert.Equal(120m, stored.Price);
        Assert.Equal(manager.Id, stored.ResolverId);

        var carNow = (await world.Cars.GetByIdAsync(car.Id))!;
        Assert.Equal(CarStatus.Rented, carNow.Status);
    }

    [Fact]
    public async Task ApproveRequest_ByNonManager_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var notAManager = await world.AddUserAsync("bob", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.ApproveAsync(request.Id, notAManager.Id));
    }

    [Fact]
    public async Task ApproveRequest_AlreadyResolved_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);

        await Assert.ThrowsAsync<RentalRequestAlreadyResolvedException>(() =>
            world.Service.ApproveAsync(request.Id, manager.Id));
    }

    // ----- Auto-approval (Client + Manager) -------------------------------

    [Fact]
    public async Task CreateRequest_ByClientManager_IsAutoApproved()
    {
        var world = new TestWorld();
        var hybrid = await world.AddUserAsync("ceo", age: 30, experience: 5, UserRole.Client, UserRole.Manager);
        var car = await world.AddCarAsync(dailyTariff: 100m);

        var request = await world.Service.CreateAsync(hybrid.Id, car.Id, D(1), D(3));

        Assert.Equal(RentalRequestStatus.Approved, request.Status);
        Assert.Equal(hybrid.Id, request.ResolverId);
        Assert.Equal(200m, request.Price);
        Assert.Equal(CarStatus.Rented, (await world.Cars.GetByIdAsync(car.Id))!.Status);
    }

    // ----- Reject ----------------------------------------------------------

    [Fact]
    public async Task RejectRequest_RecordsReasonAndDoesNotTouchCar()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.RejectAsync(request.Id, manager.Id, "client blacklisted");

        var stored = (await world.Requests.GetByIdAsync(request.Id))!;
        Assert.Equal(RentalRequestStatus.Rejected, stored.Status);
        Assert.Equal("client blacklisted", stored.RejectionReason);
        Assert.Equal(CarStatus.Available, (await world.Cars.GetByIdAsync(car.Id))!.Status);
    }

    [Fact]
    public async Task RejectRequest_ByNonManager_Throws()
    {
        var world = new TestWorld();
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();
        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.RejectAsync(request.Id, client.Id, "no reason"));
    }

    [Fact]
    public async Task RejectRequest_EmptyReason_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();
        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            world.Service.RejectAsync(request.Id, manager.Id, "   "));
    }

    // ----- Complete --------------------------------------------------------

    [Fact]
    public async Task CompleteRequest_OnTimeAndUndamaged_NoPenaltyAndCarFreed()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(dailyTariff: 40m);

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);
        await world.Service.CompleteAsync(request.Id, manager.Id, actualReturnDate: D(4), damaged: false);

        var stored = (await world.Requests.GetByIdAsync(request.Id))!;
        Assert.Equal(RentalRequestStatus.Completed, stored.Status);
        Assert.Equal(120m, stored.Price);
        Assert.Equal(0m, stored.Penalty);
        Assert.Equal(D(4), stored.ActualReturnDate);
        Assert.False(stored.Damaged);

        var carNow = (await world.Cars.GetByIdAsync(car.Id))!;
        Assert.Equal(CarStatus.Available, carNow.Status);
    }

    [Fact]
    public async Task CompleteRequest_DamagedReturn_AppliesDamagePenalty()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(dailyTariff: 40m);

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);
        await world.Service.CompleteAsync(request.Id, manager.Id, actualReturnDate: D(4), damaged: true);

        var stored = (await world.Requests.GetByIdAsync(request.Id))!;
        // base = 40 * 3 = 120; damage = 50% of 120 = 60.
        Assert.Equal(60m, stored.Penalty);
        Assert.True(stored.Damaged);
    }

    [Fact]
    public async Task CompleteRequest_LateReturn_AppliesLatePenalty()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync(dailyTariff: 40m);

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);
        await world.Service.CompleteAsync(request.Id, manager.Id, actualReturnDate: D(6), damaged: false);

        var stored = (await world.Requests.GetByIdAsync(request.Id))!;
        // 2 late days; late fee = 40 * 1.5 * 2 = 120.
        Assert.Equal(120m, stored.Penalty);
    }

    [Fact]
    public async Task CompleteRequest_OnPendingRequest_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));

        await Assert.ThrowsAsync<RentalRequestNotApprovedException>(() =>
            world.Service.CompleteAsync(request.Id, manager.Id, D(4), damaged: false));
    }

    [Fact]
    public async Task CompleteRequest_ByNonManager_Throws()
    {
        var world = new TestWorld();
        var manager = await world.AddUserAsync("mgr", roles: UserRole.Manager);
        var client = await world.AddUserAsync("alice", roles: UserRole.Client);
        var car = await world.AddCarAsync();

        var request = await world.Service.CreateAsync(client.Id, car.Id, D(1), D(4));
        await world.Service.ApproveAsync(request.Id, manager.Id);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.CompleteAsync(request.Id, client.Id, D(4), damaged: false));
    }
}

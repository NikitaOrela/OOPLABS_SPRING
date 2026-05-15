using CarRental.Application.Services;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using CarRental.Infrastructure.Persistence;
using Xunit;

namespace CarRental.Tests;

public class SmokeTests
{
    [Fact]
    public void CarStatus_ValuesStartAtOne()
    {
        Assert.Equal(1, (int)CarStatus.Available);
        Assert.Equal(2, (int)CarStatus.Rented);
        Assert.Equal(3, (int)CarStatus.UnderMaintenance);
    }

    [Fact]
    public void RentalRequestStatus_DefaultIsPending()
    {
        var request = new RentalRequest();
        Assert.Equal(RentalRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void RentalRequest_DurationDays_ComputesInclusiveSpan()
    {
        var request = new RentalRequest
        {
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 4)
        };
        Assert.Equal(3, request.DurationDays);
    }

    [Fact]
    public void RentalPricing_MultipliesDailyTariffByDays()
    {
        Assert.Equal(150m, RentalPricing.Calculate(50m, 3));
    }

    [Fact]
    public async Task InMemoryCarRepository_RejectsDuplicateVin()
    {
        var repo = new InMemoryCarRepository();
        await repo.AddAsync(new Car { Vin = "VIN123", Make = "X", Model = "Y", DailyTariff = 10m });
        await Assert.ThrowsAsync<DuplicateVinException>(() =>
            repo.AddAsync(new Car { Vin = "VIN123", Make = "A", Model = "B", DailyTariff = 20m }));
    }
}

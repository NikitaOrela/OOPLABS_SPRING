using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;

namespace CarRental.Application.Services;

// TODO: full implementation will be added during Lab 2 work.
public class RentalRequestService : IRentalRequestService
{
    public Task<RentalRequest> CreateAsync(int clientId, int carId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task ApproveAsync(int requestId, int managerId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task RejectAsync(int requestId, int managerId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task CompleteAsync(int requestId, int managerId, decimal? penalty, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

public static class RentalPricing
{
    // price = daily tariff * days, plus optional penalties (handled by caller).
    public static decimal Calculate(decimal dailyTariff, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dailyTariff);
        ArgumentOutOfRangeException.ThrowIfNegative(days);
        return dailyTariff * days;
    }
}

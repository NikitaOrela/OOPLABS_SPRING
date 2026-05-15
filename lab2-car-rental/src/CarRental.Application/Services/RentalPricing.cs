namespace CarRental.Application.Services;

// Centralised pricing. Kept as a static helper so the formula appears once and
// can be unit-tested in isolation. The penalty policy mirrors the same shape.
public static class RentalPricing
{
    // Fixed penalty for returning a damaged car (fraction of base price).
    public const decimal DamageFeeFraction = 0.5m;

    // Daily fee multiplier for every day past EndDate.
    public const decimal LateReturnDailyFraction = 1.5m;

    // price = daily tariff * planned days.
    public static decimal CalculateBase(decimal dailyTariff, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dailyTariff);
        ArgumentOutOfRangeException.ThrowIfNegative(days);
        return dailyTariff * days;
    }

    // Convenience overload kept for backwards compatibility with the smoke
    // tests written against the original skeleton.
    public static decimal Calculate(decimal dailyTariff, int days) => CalculateBase(dailyTariff, days);

    // damaged => +50% of base price; every day late => +150% of daily tariff.
    public static decimal CalculatePenalty(decimal dailyTariff, decimal basePrice, int lateDays, bool damaged)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dailyTariff);
        ArgumentOutOfRangeException.ThrowIfNegative(basePrice);
        ArgumentOutOfRangeException.ThrowIfNegative(lateDays);

        decimal penalty = 0m;
        if (damaged)
        {
            penalty += basePrice * DamageFeeFraction;
        }
        if (lateDays > 0)
        {
            penalty += dailyTariff * LateReturnDailyFraction * lateDays;
        }
        return penalty;
    }
}

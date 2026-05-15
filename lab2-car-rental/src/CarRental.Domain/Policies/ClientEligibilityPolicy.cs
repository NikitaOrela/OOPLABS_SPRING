using CarRental.Domain.Entities;

namespace CarRental.Domain.Policies;

// Centralised, deterministic eligibility rules. Kept in Domain so they are
// independent of the API/service layer and can be reused by tests as a single
// source of truth.
public static class ClientEligibilityPolicy
{
    // Base rule for any rental:
    //   age > 21 AND driving experience >= 2 years.
    public const int MinAge = 22;                  // strictly older than 21
    public const int MinExperienceYears = 2;

    // Stricter rule for "powerful" cars (>= 250 HP):
    //   age >= 25 AND driving experience >= 5 years.
    public const int PowerfulCarThresholdHp = 250;
    public const int MinAgeForPowerfulCar = 25;
    public const int MinExperienceYearsForPowerfulCar = 5;

    public static bool IsPowerful(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        return car.PowerHp >= PowerfulCarThresholdHp;
    }

    // Returns null if the client may rent the car, or a human-readable reason
    // explaining the first failed precondition otherwise.
    public static string? Check(User client, Car car)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(car);

        if (client.Age < MinAge)
        {
            return $"age must be greater than 21 (was {client.Age}).";
        }
        if (client.DrivingExperienceYears < MinExperienceYears)
        {
            return $"driving experience must be at least {MinExperienceYears} years (was {client.DrivingExperienceYears}).";
        }
        if (IsPowerful(car))
        {
            if (client.Age < MinAgeForPowerfulCar)
            {
                return $"age must be at least {MinAgeForPowerfulCar} for cars with {PowerfulCarThresholdHp}+ HP (was {client.Age}).";
            }
            if (client.DrivingExperienceYears < MinExperienceYearsForPowerfulCar)
            {
                return $"driving experience must be at least {MinExperienceYearsForPowerfulCar} years for cars with {PowerfulCarThresholdHp}+ HP (was {client.DrivingExperienceYears}).";
            }
        }
        return null;
    }
}

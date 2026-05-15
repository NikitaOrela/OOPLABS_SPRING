using CarRental.Domain.Entities;

namespace CarRental.Presentation.Contracts;

internal static class Mapping
{
    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Age = user.Age,
            DrivingExperienceYears = user.DrivingExperienceYears,
            Roles = user.Roles.ToList()
        };
    }

    public static CarResponse ToResponse(Car car)
    {
        return new CarResponse
        {
            Id = car.Id,
            Vin = car.Vin,
            Make = car.Make,
            Model = car.Model,
            PowerHp = car.PowerHp,
            DailyTariff = car.DailyTariff,
            Status = car.Status
        };
    }

    public static RentalRequestResponse ToResponse(RentalRequest request)
    {
        return new RentalRequestResponse
        {
            Id = request.Id,
            ClientId = request.ClientId,
            CarId = request.CarId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            Price = request.Price,
            Penalty = request.Penalty,
            CreatedAt = request.CreatedAt,
            ResolvedAt = request.ResolvedAt,
            ResolverId = request.ResolverId,
            RejectionReason = request.RejectionReason,
            ActualReturnDate = request.ActualReturnDate,
            Damaged = request.Damaged,
            DurationDays = request.DurationDays
        };
    }
}

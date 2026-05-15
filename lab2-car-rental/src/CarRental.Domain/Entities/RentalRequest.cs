using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public class RentalRequest
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public RentalRequestStatus Status { get; set; } = RentalRequestStatus.Pending;
    public decimal? Price { get; set; }
    public decimal? Penalty { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolverId { get; set; }

    public int DurationDays => Math.Max(0, EndDate.DayNumber - StartDate.DayNumber);
}

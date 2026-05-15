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

    // Base price = dailyTariff * planned days. Filled in when the request is
    // moved to Approved (a confirmed contract).
    public decimal? Price { get; set; }

    // Total penalty: damage fee + late-return fee. Filled in by CompleteAsync.
    public decimal? Penalty { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolverId { get; set; }

    // Filled in when the manager rejects the request.
    public string? RejectionReason { get; set; }

    // Filled in when the rental is completed (CompleteAsync).
    public DateOnly? ActualReturnDate { get; set; }
    public bool Damaged { get; set; }

    // Planned span in days (exclusive end). 2026-05-01 -> 2026-05-04 == 3 days.
    public int DurationDays => Math.Max(0, EndDate.DayNumber - StartDate.DayNumber);
}

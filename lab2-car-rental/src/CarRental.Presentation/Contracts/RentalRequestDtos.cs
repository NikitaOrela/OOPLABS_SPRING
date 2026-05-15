using CarRental.Domain.Enums;

namespace CarRental.Presentation.Contracts;

public class CreateRentalRequestRequest
{
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class ApproveRentalRequestRequest
{
    public int ManagerId { get; set; }
}

public class RejectRentalRequestRequest
{
    public int ManagerId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CompleteRentalRequestRequest
{
    public int ManagerId { get; set; }
    public DateOnly ActualReturnDate { get; set; }
    public bool Damaged { get; set; }
}

public class RentalRequestResponse
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public RentalRequestStatus Status { get; set; }
    public decimal? Price { get; set; }
    public decimal? Penalty { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolverId { get; set; }
    public string? RejectionReason { get; set; }
    public DateOnly? ActualReturnDate { get; set; }
    public bool Damaged { get; set; }
    public int DurationDays { get; set; }
}

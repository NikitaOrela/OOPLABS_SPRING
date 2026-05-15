using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class BookRequest
{
    public int Id { get; set; }
    public int ApplicantId { get; set; }
    public int BookId { get; set; }
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolverId { get; set; }
}

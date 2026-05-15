using Library.Domain.Enums;

namespace Library.Presentation.Contracts;

public class CreateBookRequestRequest
{
    public int ApplicantId { get; set; }
    public int BookId { get; set; }
    public RequestType Type { get; set; }
    public int Quantity { get; set; }
}

public class ResolveBookRequestRequest
{
    public int LibrarianId { get; set; }
}

public class BookRequestResponse
{
    public int Id { get; set; }
    public int ApplicantId { get; set; }
    public int BookId { get; set; }
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolverId { get; set; }
}

namespace Library.Domain.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int WriterId { get; set; }

    // Maximum number of copies the writer is allowed to put into circulation.
    public int Circulation { get; set; }

    // Number of copies the writer has already supplied (sum of approved Supply requests).
    // Must never exceed Circulation.
    public int SuppliedCopies { get; set; }

    // Copies currently available for readers to borrow.
    public int AvailableCopies { get; set; }

    public int RemainingCirculation => Circulation - SuppliedCopies;
}

namespace Library.Presentation.Contracts;

public class CreateBookRequest
{
    public int WriterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Circulation { get; set; }
}

public class BookResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int WriterId { get; set; }
    public int Circulation { get; set; }
    public int SuppliedCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int RemainingCirculation { get; set; }
}

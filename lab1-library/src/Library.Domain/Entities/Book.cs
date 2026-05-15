namespace Library.Domain.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int WriterId { get; set; }
    public int Circulation { get; set; }
    public int AvailableCopies { get; set; }
}

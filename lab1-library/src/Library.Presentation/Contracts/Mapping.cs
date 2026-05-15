using Library.Domain.Entities;

namespace Library.Presentation.Contracts;

internal static class Mapping
{
    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Roles = user.Roles.ToList()
        };
    }

    public static BookResponse ToResponse(Book book)
    {
        return new BookResponse
        {
            Id = book.Id,
            Title = book.Title,
            WriterId = book.WriterId,
            Circulation = book.Circulation,
            SuppliedCopies = book.SuppliedCopies,
            AvailableCopies = book.AvailableCopies,
            RemainingCirculation = book.RemainingCirculation
        };
    }

    public static BookRequestResponse ToResponse(BookRequest request)
    {
        return new BookRequestResponse
        {
            Id = request.Id,
            ApplicantId = request.ApplicantId,
            BookId = request.BookId,
            Type = request.Type,
            Status = request.Status,
            Quantity = request.Quantity,
            CreatedAt = request.CreatedAt,
            ResolvedAt = request.ResolvedAt,
            ResolverId = request.ResolverId
        };
    }
}

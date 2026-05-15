using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Domain.Interfaces;

namespace Library.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly IUserRepository _users;

    public BookService(IBookRepository books, IUserRepository users)
    {
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(users);
        _books = books;
        _users = users;
    }

    public async Task<Book> CreateAsync(
        int writerId,
        string title,
        int circulation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }
        if (circulation <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(circulation), "Circulation must be positive.");
        }

        var writer = await _users.GetByIdAsync(writerId, cancellationToken)
            ?? throw new UserNotFoundException(writerId);
        if (!writer.HasRole(UserRole.Writer))
        {
            throw new UnauthorizedRoleException($"User {writerId} is not a writer and cannot register books.");
        }

        var book = new Book
        {
            Title = title,
            WriterId = writer.Id,
            Circulation = circulation,
            SuppliedCopies = 0,
            AvailableCopies = 0
        };
        await _books.AddAsync(book, cancellationToken);
        return book;
    }

    public Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _books.GetByIdAsync(id, cancellationToken);
    }
}

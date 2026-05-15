using Library.Domain.Entities;
using Library.Domain.Interfaces;

namespace Library.Infrastructure.Persistence;

// Placeholder in-memory implementation. EF Core context will replace this in a later iteration.
public class InMemoryBookRepository : IBookRepository
{
    private readonly Dictionary<int, Book> _byId = new();
    private int _nextId = 1;

    public Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var book);
        return Task.FromResult(book);
    }

    public Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (book.Id == 0)
        {
            book.Id = _nextId++;
        }
        else if (book.Id >= _nextId)
        {
            _nextId = book.Id + 1;
        }
        _byId[book.Id] = book;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (!_byId.ContainsKey(book.Id))
        {
            throw new KeyNotFoundException($"Book {book.Id} was not found.");
        }
        _byId[book.Id] = book;
        return Task.CompletedTask;
    }
}

using Library.Domain.Entities;

namespace Library.Application.Abstractions;

public interface IBookService
{
    Task<Book> CreateAsync(int writerId, string title, int circulation, CancellationToken cancellationToken = default);
    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

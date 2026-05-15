using Library.Domain.Entities;

namespace Library.Domain.Interfaces;

public interface IBookRequestRepository
{
    Task<BookRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(BookRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(BookRequest request, CancellationToken cancellationToken = default);
    Task<bool> ReaderHasEverBorrowedAsync(int readerId, int bookId, CancellationToken cancellationToken = default);
}

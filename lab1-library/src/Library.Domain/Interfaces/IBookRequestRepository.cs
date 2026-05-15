using Library.Domain.Entities;

namespace Library.Domain.Interfaces;

public interface IBookRequestRepository
{
    Task<BookRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(BookRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(BookRequest request, CancellationToken cancellationToken = default);

    // True if the reader has ever had an approved Receive request for the given book,
    // regardless of whether it was subsequently returned. Used to enforce that a reader
    // cannot request a book they have already borrowed in the past.
    Task<bool> ReaderHasEverBorrowedAsync(int readerId, int bookId, CancellationToken cancellationToken = default);

    // True if the reader currently holds the book — i.e. has an approved Receive without
    // a matching approved Return. Used to validate that a return request corresponds to an
    // active loan.
    Task<bool> ReaderCurrentlyHoldsAsync(int readerId, int bookId, CancellationToken cancellationToken = default);
}

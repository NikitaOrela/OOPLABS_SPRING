using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Services;

// TODO: full implementation will be added during Lab 1 work.
// Skeleton only — keeps Application layer compile-safe.
public class BookRequestService : IBookRequestService
{
    public Task<BookRequest> CreateAsync(int applicantId, int bookId, RequestType type, int quantity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ApproveAsync(int requestId, int librarianId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RejectAsync(int requestId, int librarianId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

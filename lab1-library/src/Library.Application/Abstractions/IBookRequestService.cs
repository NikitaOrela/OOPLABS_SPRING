using Library.Domain.Entities;
using Library.Domain.Enums;

namespace Library.Application.Abstractions;

public interface IBookRequestService
{
    Task<BookRequest> CreateAsync(int applicantId, int bookId, RequestType type, int quantity, CancellationToken cancellationToken = default);
    Task ApproveAsync(int requestId, int librarianId, CancellationToken cancellationToken = default);
    Task RejectAsync(int requestId, int librarianId, CancellationToken cancellationToken = default);
}

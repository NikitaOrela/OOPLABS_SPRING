using Library.Application.Abstractions;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Domain.Interfaces;

namespace Library.Application.Services;

public class BookRequestService : IBookRequestService
{
    private readonly IUserRepository _users;
    private readonly IBookRepository _books;
    private readonly IBookRequestRepository _requests;
    private readonly IClock _clock;

    public BookRequestService(
        IUserRepository users,
        IBookRepository books,
        IBookRequestRepository requests,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(books);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(clock);
        _users = users;
        _books = books;
        _requests = requests;
        _clock = clock;
    }

    public async Task<BookRequest> CreateAsync(
        int applicantId,
        int bookId,
        RequestType type,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }
        if (!Enum.IsDefined(typeof(RequestType), type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Unknown request type.");
        }

        var applicant = await _users.GetByIdAsync(applicantId, cancellationToken)
            ?? throw new UserNotFoundException(applicantId);
        var book = await _books.GetByIdAsync(bookId, cancellationToken)
            ?? throw new BookNotFoundException(bookId);

        switch (type)
        {
            case RequestType.Receive:
                await ValidateReceiveAsync(applicant, book, quantity, cancellationToken);
                break;
            case RequestType.Supply:
                ValidateSupply(applicant, book, quantity);
                break;
            case RequestType.Return:
                await ValidateReturnAsync(applicant, book, quantity, cancellationToken);
                break;
        }

        var request = new BookRequest
        {
            ApplicantId = applicant.Id,
            BookId = book.Id,
            Type = type,
            Quantity = quantity,
            Status = RequestStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        // Rule: if the applicant is also a librarian, the request is auto-approved.
        if (applicant.HasRole(UserRole.Librarian))
        {
            ApplySideEffects(request, book);
            request.Status = RequestStatus.Approved;
            request.ResolverId = applicant.Id;
            request.ResolvedAt = _clock.UtcNow;
            await _books.UpdateAsync(book, cancellationToken);
        }

        await _requests.AddAsync(request, cancellationToken);
        return request;
    }

    public async Task ApproveAsync(int requestId, int librarianId, CancellationToken cancellationToken = default)
    {
        var (request, librarian) = await LoadForResolutionAsync(requestId, librarianId, cancellationToken);
        var book = await _books.GetByIdAsync(request.BookId, cancellationToken)
            ?? throw new BookNotFoundException(request.BookId);

        // Re-validate at approval time: state may have changed between creation and approval.
        switch (request.Type)
        {
            case RequestType.Receive:
                if (book.AvailableCopies < request.Quantity)
                {
                    throw new BookNotAvailableException(book.Id);
                }
                break;
            case RequestType.Supply:
                if (book.RemainingCirculation < request.Quantity)
                {
                    throw new WriterSupplyLimitException(book.Id, request.Quantity, book.RemainingCirculation);
                }
                break;
            case RequestType.Return:
                break;
        }

        ApplySideEffects(request, book);
        request.Status = RequestStatus.Approved;
        request.ResolverId = librarian.Id;
        request.ResolvedAt = _clock.UtcNow;

        await _books.UpdateAsync(book, cancellationToken);
        await _requests.UpdateAsync(request, cancellationToken);
    }

    public async Task RejectAsync(int requestId, int librarianId, CancellationToken cancellationToken = default)
    {
        var (request, librarian) = await LoadForResolutionAsync(requestId, librarianId, cancellationToken);

        request.Status = RequestStatus.Rejected;
        request.ResolverId = librarian.Id;
        request.ResolvedAt = _clock.UtcNow;

        await _requests.UpdateAsync(request, cancellationToken);
    }

    private async Task<(BookRequest Request, User Librarian)> LoadForResolutionAsync(
        int requestId,
        int librarianId,
        CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new RequestNotFoundException(requestId);
        if (request.Status != RequestStatus.Pending)
        {
            throw new RequestAlreadyResolvedException(requestId);
        }
        var librarian = await _users.GetByIdAsync(librarianId, cancellationToken)
            ?? throw new UserNotFoundException(librarianId);
        if (!librarian.HasRole(UserRole.Librarian))
        {
            throw new UnauthorizedRoleException($"User {librarianId} is not a librarian and cannot resolve requests.");
        }
        return (request, librarian);
    }

    private async Task ValidateReceiveAsync(User applicant, Book book, int quantity, CancellationToken cancellationToken)
    {
        if (!applicant.HasRole(UserRole.Reader))
        {
            throw new UnauthorizedRoleException($"User {applicant.Id} is not a reader and cannot receive books.");
        }
        if (quantity != 1)
        {
            throw new InvalidRequestException("A reader can receive only one copy per request.");
        }
        if (book.AvailableCopies < quantity)
        {
            throw new BookNotAvailableException(book.Id);
        }
        if (await _requests.ReaderHasEverBorrowedAsync(applicant.Id, book.Id, cancellationToken))
        {
            throw new ReaderAlreadyBorrowedException(applicant.Id, book.Id);
        }
    }

    private static void ValidateSupply(User applicant, Book book, int quantity)
    {
        if (!applicant.HasRole(UserRole.Writer))
        {
            throw new UnauthorizedRoleException($"User {applicant.Id} is not a writer and cannot supply books.");
        }
        if (book.WriterId != applicant.Id)
        {
            throw new UnauthorizedRoleException($"Writer {applicant.Id} cannot supply book {book.Id} written by someone else.");
        }
        if (quantity > book.RemainingCirculation)
        {
            throw new WriterSupplyLimitException(book.Id, quantity, book.RemainingCirculation);
        }
    }

    private async Task ValidateReturnAsync(User applicant, Book book, int quantity, CancellationToken cancellationToken)
    {
        if (!applicant.HasRole(UserRole.Reader))
        {
            throw new UnauthorizedRoleException($"User {applicant.Id} is not a reader and cannot return books.");
        }
        if (quantity != 1)
        {
            throw new InvalidRequestException("A reader returns exactly one copy per request.");
        }
        if (!await _requests.ReaderCurrentlyHoldsAsync(applicant.Id, book.Id, cancellationToken))
        {
            throw new InvalidRequestException($"Reader {applicant.Id} does not currently hold book {book.Id}.");
        }
    }

    // Mutates book in-memory; caller is responsible for persisting via UpdateAsync.
    private static void ApplySideEffects(BookRequest request, Book book)
    {
        switch (request.Type)
        {
            case RequestType.Receive:
                if (book.AvailableCopies < request.Quantity)
                {
                    throw new BookNotAvailableException(book.Id);
                }
                book.AvailableCopies -= request.Quantity;
                break;
            case RequestType.Supply:
                if (book.RemainingCirculation < request.Quantity)
                {
                    throw new WriterSupplyLimitException(book.Id, request.Quantity, book.RemainingCirculation);
                }
                book.SuppliedCopies += request.Quantity;
                book.AvailableCopies += request.Quantity;
                break;
            case RequestType.Return:
                book.AvailableCopies += request.Quantity;
                break;
        }
    }
}

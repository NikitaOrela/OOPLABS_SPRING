using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Interfaces;

namespace Library.Infrastructure.Persistence;

// Placeholder in-memory implementation. EF Core context will replace this in a later iteration.
public class InMemoryBookRequestRepository : IBookRequestRepository
{
    private readonly Dictionary<int, BookRequest> _byId = new();
    private int _nextId = 1;

    public Task<BookRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var request);
        return Task.FromResult(request);
    }

    public Task AddAsync(BookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id == 0)
        {
            request.Id = _nextId++;
        }
        else if (request.Id >= _nextId)
        {
            _nextId = request.Id + 1;
        }
        _byId[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BookRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!_byId.ContainsKey(request.Id))
        {
            throw new KeyNotFoundException($"Book request {request.Id} was not found.");
        }
        _byId[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<bool> ReaderHasEverBorrowedAsync(int readerId, int bookId, CancellationToken cancellationToken = default)
    {
        foreach (var request in _byId.Values)
        {
            if (request.ApplicantId == readerId
                && request.BookId == bookId
                && request.Type == RequestType.Receive
                && request.Status == RequestStatus.Approved)
            {
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public Task<bool> ReaderCurrentlyHoldsAsync(int readerId, int bookId, CancellationToken cancellationToken = default)
    {
        int receives = 0;
        int returns = 0;
        foreach (var request in _byId.Values)
        {
            if (request.ApplicantId != readerId || request.BookId != bookId)
            {
                continue;
            }
            if (request.Status != RequestStatus.Approved)
            {
                continue;
            }
            if (request.Type == RequestType.Receive)
            {
                receives++;
            }
            else if (request.Type == RequestType.Return)
            {
                returns++;
            }
        }
        return Task.FromResult(receives > returns);
    }
}

using Library.Application.Abstractions;
using Library.Application.Services;
using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Infrastructure.Persistence;
using Xunit;

namespace Library.Tests;

public class BookRequestServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class TestWorld
    {
        public InMemoryUserRepository Users { get; } = new();
        public InMemoryBookRepository Books { get; } = new();
        public InMemoryBookRequestRepository Requests { get; } = new();
        public FixedClock Clock { get; } = new();
        public BookRequestService Service { get; }

        public TestWorld()
        {
            Service = new BookRequestService(Users, Books, Requests, Clock);
        }

        public async Task<User> AddUserAsync(string name, params UserRole[] roles)
        {
            var user = new User { UserName = name, FullName = name };
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
            await Users.AddAsync(user);
            return user;
        }

        public async Task<Book> AddBookAsync(string title, int writerId, int circulation, int available = 0, int supplied = 0)
        {
            var book = new Book
            {
                Title = title,
                WriterId = writerId,
                Circulation = circulation,
                SuppliedCopies = supplied,
                AvailableCopies = available
            };
            await Books.AddAsync(book);
            return book;
        }
    }

    // ----- Receive ---------------------------------------------------------

    [Fact]
    public async Task CreateReceive_HappyPath_CreatesPendingRequest()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, circulation: 5, available: 3, supplied: 5);

        var request = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);

        Assert.Equal(RequestStatus.Pending, request.Status);
        Assert.Equal(reader.Id, request.ApplicantId);
        Assert.Equal(book.Id, request.BookId);
        Assert.Equal(1, request.Quantity);
        Assert.Null(request.ResolverId);
        Assert.Null(request.ResolvedAt);
        // Available copies untouched until approval.
        Assert.Equal(3, (await world.Books.GetByIdAsync(book.Id))!.AvailableCopies);
    }

    [Fact]
    public async Task CreateReceive_AsNonReader_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 1, supplied: 1);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.CreateAsync(writer.Id, book.Id, RequestType.Receive, 1));
    }

    [Fact]
    public async Task CreateReceive_WhenNoCopiesAvailable_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 0, supplied: 5);

        await Assert.ThrowsAsync<BookNotAvailableException>(() =>
            world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1));
    }

    [Fact]
    public async Task CreateReceive_AfterPreviousBorrow_ThrowsEvenAfterReturn()
    {
        var world = new TestWorld();
        var librarian = await world.AddUserAsync("lib", UserRole.Librarian);
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        // First borrow + return cycle.
        var first = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);
        await world.Service.ApproveAsync(first.Id, librarian.Id);
        var ret = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Return, 1);
        await world.Service.ApproveAsync(ret.Id, librarian.Id);

        // Second receive must be blocked — reader has already borrowed it before.
        await Assert.ThrowsAsync<ReaderAlreadyBorrowedException>(() =>
            world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1));
    }

    [Fact]
    public async Task CreateReceive_QuantityGreaterThanOne_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 2));
    }

    [Fact]
    public async Task CreateReceive_ByLibrarianReader_AutoApprovesAndDecrementsCopies()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var librarianReader = await world.AddUserAsync("mixed", UserRole.Librarian, UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var request = await world.Service.CreateAsync(librarianReader.Id, book.Id, RequestType.Receive, 1);

        Assert.Equal(RequestStatus.Approved, request.Status);
        Assert.Equal(librarianReader.Id, request.ResolverId);
        Assert.NotNull(request.ResolvedAt);
        Assert.Equal(2, (await world.Books.GetByIdAsync(book.Id))!.AvailableCopies);
    }

    // ----- Supply ----------------------------------------------------------

    [Fact]
    public async Task CreateSupply_OwnBook_WithinCirculation_Pending()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, circulation: 10, available: 0, supplied: 0);

        var request = await world.Service.CreateAsync(writer.Id, book.Id, RequestType.Supply, 4);

        Assert.Equal(RequestStatus.Pending, request.Status);
        var stored = (await world.Books.GetByIdAsync(book.Id))!;
        Assert.Equal(0, stored.SuppliedCopies);
        Assert.Equal(0, stored.AvailableCopies);
    }

    [Fact]
    public async Task CreateSupply_OtherWritersBook_Throws()
    {
        var world = new TestWorld();
        var writerA = await world.AddUserAsync("writerA", UserRole.Writer);
        var writerB = await world.AddUserAsync("writerB", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writerA.Id, 10);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.CreateAsync(writerB.Id, book.Id, RequestType.Supply, 1));
    }

    [Fact]
    public async Task CreateSupply_ExceedsCirculation_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, circulation: 10, supplied: 8);

        await Assert.ThrowsAsync<WriterSupplyLimitException>(() =>
            world.Service.CreateAsync(writer.Id, book.Id, RequestType.Supply, 3));
    }

    [Fact]
    public async Task ApproveSupply_IncrementsSuppliedAndAvailableCopies()
    {
        var world = new TestWorld();
        var librarian = await world.AddUserAsync("lib", UserRole.Librarian);
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, circulation: 10);

        var supply = await world.Service.CreateAsync(writer.Id, book.Id, RequestType.Supply, 4);
        await world.Service.ApproveAsync(supply.Id, librarian.Id);

        var stored = (await world.Books.GetByIdAsync(book.Id))!;
        Assert.Equal(4, stored.SuppliedCopies);
        Assert.Equal(4, stored.AvailableCopies);
        Assert.Equal(RequestStatus.Approved, (await world.Requests.GetByIdAsync(supply.Id))!.Status);
    }

    // ----- Return ----------------------------------------------------------

    [Fact]
    public async Task CreateReturn_WithoutActiveLoan_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 1, supplied: 5);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            world.Service.CreateAsync(reader.Id, book.Id, RequestType.Return, 1));
    }

    [Fact]
    public async Task ApproveReturn_IncrementsAvailableCopies()
    {
        var world = new TestWorld();
        var librarian = await world.AddUserAsync("lib", UserRole.Librarian);
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var receive = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);
        await world.Service.ApproveAsync(receive.Id, librarian.Id);
        Assert.Equal(2, (await world.Books.GetByIdAsync(book.Id))!.AvailableCopies);

        var ret = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Return, 1);
        await world.Service.ApproveAsync(ret.Id, librarian.Id);
        Assert.Equal(3, (await world.Books.GetByIdAsync(book.Id))!.AvailableCopies);
    }

    // ----- Approve / Reject -----------------------------------------------

    [Fact]
    public async Task Approve_ByNonLibrarian_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var nobody = await world.AddUserAsync("nobody", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var receive = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.ApproveAsync(receive.Id, nobody.Id));
    }

    [Fact]
    public async Task Approve_AlreadyResolved_Throws()
    {
        var world = new TestWorld();
        var librarian = await world.AddUserAsync("lib", UserRole.Librarian);
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var receive = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);
        await world.Service.ApproveAsync(receive.Id, librarian.Id);

        await Assert.ThrowsAsync<RequestAlreadyResolvedException>(() =>
            world.Service.ApproveAsync(receive.Id, librarian.Id));
    }

    [Fact]
    public async Task Reject_SetsRejectedAndDoesNotChangeBook()
    {
        var world = new TestWorld();
        var librarian = await world.AddUserAsync("lib", UserRole.Librarian);
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var receive = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);
        await world.Service.RejectAsync(receive.Id, librarian.Id);

        var stored = (await world.Requests.GetByIdAsync(receive.Id))!;
        Assert.Equal(RequestStatus.Rejected, stored.Status);
        Assert.Equal(librarian.Id, stored.ResolverId);
        Assert.Equal(3, (await world.Books.GetByIdAsync(book.Id))!.AvailableCopies);
        // Reader is free to request again because the prior receive was never approved.
        var retry = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);
        Assert.Equal(RequestStatus.Pending, retry.Status);
    }

    [Fact]
    public async Task Reject_ByNonLibrarian_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var reader = await world.AddUserAsync("reader", UserRole.Reader);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 3, supplied: 5);

        var receive = await world.Service.CreateAsync(reader.Id, book.Id, RequestType.Receive, 1);

        await Assert.ThrowsAsync<UnauthorizedRoleException>(() =>
            world.Service.RejectAsync(receive.Id, reader.Id));
    }

    // ----- Validation ------------------------------------------------------

    [Fact]
    public async Task Create_WithUnknownApplicant_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, 5, available: 1, supplied: 5);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            world.Service.CreateAsync(999, book.Id, RequestType.Receive, 1));
    }

    [Fact]
    public async Task Create_WithUnknownBook_Throws()
    {
        var world = new TestWorld();
        var reader = await world.AddUserAsync("reader", UserRole.Reader);

        await Assert.ThrowsAsync<BookNotFoundException>(() =>
            world.Service.CreateAsync(reader.Id, 999, RequestType.Receive, 1));
    }

    [Fact]
    public async Task Create_WithNonPositiveQuantity_Throws()
    {
        var world = new TestWorld();
        var writer = await world.AddUserAsync("writer", UserRole.Writer);
        var book = await world.AddBookAsync("Dune", writer.Id, 5);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            world.Service.CreateAsync(writer.Id, book.Id, RequestType.Supply, 0));
    }
}

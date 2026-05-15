using System.Net;
using System.Net.Http.Json;
using Library.Domain.Enums;
using Library.Presentation;
using Library.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Library.Tests;

public class LibraryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LibraryApiTests(WebApplicationFactory<Program> factory)
    {
        // Each test class instance gets a fresh host => isolated in-memory state.
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    private HttpClient NewClient() => _factory.CreateClient();

    [Fact]
    public async Task FullHappyPath_CreateUsers_Book_SupplyAndReceive()
    {
        var client = NewClient();

        var librarian = await CreateUserAsync(client, "lib", "Lib Larian", UserRole.Librarian);
        var writer = await CreateUserAsync(client, "writer", "W Riter", UserRole.Writer);
        var reader = await CreateUserAsync(client, "reader", "R Eader", UserRole.Reader);

        var book = await CreateBookAsync(client, writer.Id, "Dune", circulation: 5);
        Assert.Equal(0, book.SuppliedCopies);
        Assert.Equal(0, book.AvailableCopies);

        // Writer supplies 3 of 5 — Pending until librarian approves.
        var supply = await CreateRequestAsync(client, writer.Id, book.Id, RequestType.Supply, 3);
        Assert.Equal(RequestStatus.Pending, supply.Status);
        await ApproveAsync(client, supply.Id, librarian.Id);

        var afterSupply = await GetBookAsync(client, book.Id);
        Assert.Equal(3, afterSupply.SuppliedCopies);
        Assert.Equal(3, afterSupply.AvailableCopies);
        Assert.Equal(2, afterSupply.RemainingCirculation);

        // Reader receives one copy.
        var receive = await CreateRequestAsync(client, reader.Id, book.Id, RequestType.Receive, 1);
        await ApproveAsync(client, receive.Id, librarian.Id);

        var afterReceive = await GetBookAsync(client, book.Id);
        Assert.Equal(2, afterReceive.AvailableCopies);

        // Reader returns it.
        var ret = await CreateRequestAsync(client, reader.Id, book.Id, RequestType.Return, 1);
        await ApproveAsync(client, ret.Id, librarian.Id);

        var afterReturn = await GetBookAsync(client, book.Id);
        Assert.Equal(3, afterReturn.AvailableCopies);
    }

    [Fact]
    public async Task DuplicateUserName_Returns409()
    {
        var client = NewClient();
        await CreateUserAsync(client, "alice", "Alice", UserRole.Reader);
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            UserName = "alice",
            FullName = "Alice Two",
            Roles = new List<UserRole> { UserRole.Reader }
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReaderRepeatBorrow_Returns422()
    {
        var client = NewClient();
        var librarian = await CreateUserAsync(client, "lib", "Lib", UserRole.Librarian);
        var writer = await CreateUserAsync(client, "writer", "Writer", UserRole.Writer);
        var reader = await CreateUserAsync(client, "reader", "Reader", UserRole.Reader);
        var book = await CreateBookAsync(client, writer.Id, "Dune", 5);

        var supply = await CreateRequestAsync(client, writer.Id, book.Id, RequestType.Supply, 2);
        await ApproveAsync(client, supply.Id, librarian.Id);

        var first = await CreateRequestAsync(client, reader.Id, book.Id, RequestType.Receive, 1);
        await ApproveAsync(client, first.Id, librarian.Id);

        var ret = await CreateRequestAsync(client, reader.Id, book.Id, RequestType.Return, 1);
        await ApproveAsync(client, ret.Id, librarian.Id);

        var repeat = await client.PostAsJsonAsync("/api/requests", new CreateBookRequestRequest
        {
            ApplicantId = reader.Id,
            BookId = book.Id,
            Type = RequestType.Receive,
            Quantity = 1
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, repeat.StatusCode);
    }

    [Fact]
    public async Task WriterSupplyOverCirculation_Returns422()
    {
        var client = NewClient();
        var writer = await CreateUserAsync(client, "writer", "Writer", UserRole.Writer);
        var book = await CreateBookAsync(client, writer.Id, "Dune", 5);

        var response = await client.PostAsJsonAsync("/api/requests", new CreateBookRequestRequest
        {
            ApplicantId = writer.Id,
            BookId = book.Id,
            Type = RequestType.Supply,
            Quantity = 10
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ResolveByNonLibrarian_Returns403()
    {
        var client = NewClient();
        var writer = await CreateUserAsync(client, "writer", "Writer", UserRole.Writer);
        var reader = await CreateUserAsync(client, "reader", "Reader", UserRole.Reader);
        var librarian = await CreateUserAsync(client, "lib", "Lib", UserRole.Librarian);
        var book = await CreateBookAsync(client, writer.Id, "Dune", 5);
        var supply = await CreateRequestAsync(client, writer.Id, book.Id, RequestType.Supply, 2);
        await ApproveAsync(client, supply.Id, librarian.Id);

        var receive = await CreateRequestAsync(client, reader.Id, book.Id, RequestType.Receive, 1);
        var response = await client.PostAsJsonAsync(
            $"/api/requests/{receive.Id}/approve",
            new ResolveBookRequestRequest { LibrarianId = reader.Id });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LibrarianReader_AutoApprovesReceive()
    {
        var client = NewClient();
        var writer = await CreateUserAsync(client, "writer", "Writer", UserRole.Writer);
        var librarian = await CreateUserAsync(client, "lib", "Lib", UserRole.Librarian);
        var combo = await CreateUserAsync(client, "combo", "Combo", UserRole.Librarian, UserRole.Reader);

        var book = await CreateBookAsync(client, writer.Id, "Dune", 5);
        var supply = await CreateRequestAsync(client, writer.Id, book.Id, RequestType.Supply, 2);
        await ApproveAsync(client, supply.Id, librarian.Id);

        var receive = await CreateRequestAsync(client, combo.Id, book.Id, RequestType.Receive, 1);
        Assert.Equal(RequestStatus.Approved, receive.Status);
        Assert.Equal(combo.Id, receive.ResolverId);

        var after = await GetBookAsync(client, book.Id);
        Assert.Equal(1, after.AvailableCopies);
    }

    // ---- helpers ----------------------------------------------------------

    private static async Task<UserResponse> CreateUserAsync(HttpClient client, string userName, string fullName, params UserRole[] roles)
    {
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            UserName = userName,
            FullName = fullName,
            Roles = roles.ToList()
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private static async Task<BookResponse> CreateBookAsync(HttpClient client, int writerId, string title, int circulation)
    {
        var response = await client.PostAsJsonAsync("/api/books", new CreateBookRequest
        {
            WriterId = writerId,
            Title = title,
            Circulation = circulation
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookResponse>())!;
    }

    private static async Task<BookResponse> GetBookAsync(HttpClient client, int id)
    {
        var response = await client.GetAsync($"/api/books/{id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookResponse>())!;
    }

    private static async Task<BookRequestResponse> CreateRequestAsync(HttpClient client, int applicantId, int bookId, RequestType type, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/requests", new CreateBookRequestRequest
        {
            ApplicantId = applicantId,
            BookId = bookId,
            Type = type,
            Quantity = quantity
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookRequestResponse>())!;
    }

    private static async Task<BookRequestResponse> ApproveAsync(HttpClient client, int requestId, int librarianId)
    {
        var response = await client.PostAsJsonAsync($"/api/requests/{requestId}/approve", new ResolveBookRequestRequest
        {
            LibrarianId = librarianId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookRequestResponse>())!;
    }
}

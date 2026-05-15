using Library.Domain.Entities;
using Library.Domain.Enums;
using Library.Domain.Exceptions;
using Library.Infrastructure.Persistence;
using Xunit;

namespace Library.Tests;

public class SmokeTests
{
    [Fact]
    public void UserRole_ValuesStartAtOne()
    {
        Assert.Equal(1, (int)UserRole.Librarian);
        Assert.Equal(2, (int)UserRole.Writer);
        Assert.Equal(3, (int)UserRole.Reader);
    }

    [Fact]
    public void RequestStatus_DefaultIsPending()
    {
        var request = new BookRequest();
        Assert.Equal(RequestStatus.Pending, request.Status);
    }

    [Fact]
    public void User_HasRole_ReportsMembership()
    {
        var user = new User { UserName = "alice", FullName = "Alice" };
        user.Roles.Add(UserRole.Reader);
        Assert.True(user.HasRole(UserRole.Reader));
        Assert.False(user.HasRole(UserRole.Librarian));
    }

    [Fact]
    public async Task InMemoryUserRepository_RejectsDuplicateUserName()
    {
        var repo = new InMemoryUserRepository();
        await repo.AddAsync(new User { UserName = "bob", FullName = "Bob" });
        await Assert.ThrowsAsync<DuplicateUserNameException>(() =>
            repo.AddAsync(new User { UserName = "bob", FullName = "Bobby" }));
    }
}

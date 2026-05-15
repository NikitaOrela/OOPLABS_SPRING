namespace Library.Domain.Exceptions;

public class LibraryDomainException : Exception
{
    public LibraryDomainException(string message) : base(message) { }
    public LibraryDomainException(string message, Exception inner) : base(message, inner) { }
}

public class DuplicateUserNameException : LibraryDomainException
{
    public DuplicateUserNameException(string userName)
        : base($"User name '{userName}' is already taken.") { }
}

public class InvalidRequestException : LibraryDomainException
{
    public InvalidRequestException(string message) : base(message) { }
}

public class BookNotAvailableException : LibraryDomainException
{
    public BookNotAvailableException(int bookId)
        : base($"Book {bookId} is not available for this operation.") { }
}

public class UserNotFoundException : LibraryDomainException
{
    public UserNotFoundException(int userId)
        : base($"User {userId} was not found.") { }
}

public class BookNotFoundException : LibraryDomainException
{
    public BookNotFoundException(int bookId)
        : base($"Book {bookId} was not found.") { }
}

public class RequestNotFoundException : LibraryDomainException
{
    public RequestNotFoundException(int requestId)
        : base($"Book request {requestId} was not found.") { }
}

public class UnauthorizedRoleException : LibraryDomainException
{
    public UnauthorizedRoleException(string message) : base(message) { }
}

public class WriterSupplyLimitException : LibraryDomainException
{
    public WriterSupplyLimitException(int bookId, int requested, int remaining)
        : base($"Writer cannot supply {requested} copies of book {bookId}: only {remaining} remain within the declared circulation.") { }
}

public class ReaderAlreadyBorrowedException : LibraryDomainException
{
    public ReaderAlreadyBorrowedException(int readerId, int bookId)
        : base($"Reader {readerId} has already borrowed book {bookId} before and cannot request it again.") { }
}

public class RequestAlreadyResolvedException : LibraryDomainException
{
    public RequestAlreadyResolvedException(int requestId)
        : base($"Book request {requestId} is already resolved and cannot be changed.") { }
}

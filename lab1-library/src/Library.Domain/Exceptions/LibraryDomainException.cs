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

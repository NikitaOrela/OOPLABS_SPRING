namespace CarRental.Domain.Exceptions;

public class CarRentalDomainException : Exception
{
    public CarRentalDomainException(string message) : base(message) { }
    public CarRentalDomainException(string message, Exception inner) : base(message, inner) { }
}

public class DuplicateVinException : CarRentalDomainException
{
    public DuplicateVinException(string vin)
        : base($"VIN '{vin}' is already registered.") { }
}

public class DuplicateUserNameException : CarRentalDomainException
{
    public DuplicateUserNameException(string userName)
        : base($"User name '{userName}' is already taken.") { }
}

public class UserNotFoundException : CarRentalDomainException
{
    public UserNotFoundException(int userId)
        : base($"User {userId} was not found.") { }
}

public class CarNotFoundException : CarRentalDomainException
{
    public CarNotFoundException(int carId)
        : base($"Car {carId} was not found.") { }
}

public class RentalRequestNotFoundException : CarRentalDomainException
{
    public RentalRequestNotFoundException(int requestId)
        : base($"Rental request {requestId} was not found.") { }
}

public class UnauthorizedRoleException : CarRentalDomainException
{
    public UnauthorizedRoleException(string message) : base(message) { }
}

public class InvalidRentalRequestException : CarRentalDomainException
{
    public InvalidRentalRequestException(string message) : base(message) { }
}

public class CarNotAvailableException : CarRentalDomainException
{
    public CarNotAvailableException(int carId)
        : base($"Car {carId} is not available for the requested dates.") { }

    public CarNotAvailableException(int carId, string reason)
        : base($"Car {carId} is not available: {reason}") { }
}

public class ClientNotEligibleException : CarRentalDomainException
{
    public ClientNotEligibleException(string reason)
        : base($"Client is not eligible: {reason}") { }
}

public class RentalRequestAlreadyResolvedException : CarRentalDomainException
{
    public RentalRequestAlreadyResolvedException(int requestId)
        : base($"Rental request {requestId} is already resolved and cannot be changed.") { }
}

public class RentalRequestNotApprovedException : CarRentalDomainException
{
    public RentalRequestNotApprovedException(int requestId)
        : base($"Rental request {requestId} must be Approved before it can be completed.") { }
}

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

public class CarNotAvailableException : CarRentalDomainException
{
    public CarNotAvailableException(int carId)
        : base($"Car {carId} is not available for the requested dates.") { }
}

public class ClientNotEligibleException : CarRentalDomainException
{
    public ClientNotEligibleException(string reason)
        : base($"Client is not eligible: {reason}") { }
}

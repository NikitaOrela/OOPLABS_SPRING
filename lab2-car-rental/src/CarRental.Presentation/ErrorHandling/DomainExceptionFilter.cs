using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarRental.Presentation.ErrorHandling;

// Translates domain and argument exceptions into clean HTTP responses,
// keeping controllers free of try/catch boilerplate.
public class DomainExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, title) = Resolve(context.Exception);
        if (statusCode == 0)
        {
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }

    private static (int StatusCode, string Title) Resolve(Exception exception)
    {
        switch (exception)
        {
            case UserNotFoundException:
            case CarNotFoundException:
            case RentalRequestNotFoundException:
                return (StatusCodes.Status404NotFound, "Resource not found");

            case DuplicateUserNameException:
            case DuplicateVinException:
            case RentalRequestAlreadyResolvedException:
                return (StatusCodes.Status409Conflict, "Conflict");

            case UnauthorizedRoleException:
                return (StatusCodes.Status403Forbidden, "Operation not allowed for this role");

            case ClientNotEligibleException:
            case CarNotAvailableException:
            case InvalidRentalRequestException:
            case RentalRequestNotApprovedException:
                return (StatusCodes.Status422UnprocessableEntity, "Business rule violation");

            case CarRentalDomainException:
                return (StatusCodes.Status400BadRequest, "Domain error");

            case ArgumentException:
                return (StatusCodes.Status400BadRequest, "Invalid argument");

            default:
                return (0, string.Empty);
        }
    }
}

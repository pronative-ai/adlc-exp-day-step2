using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace OuterloopLabApi.Exceptions;

public static class ExceptionMapping
{
    public static ProblemHttpResult ToProblemHttpResult(Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            DomainValidationException validationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid conversion request",
                Detail = validationException.Message,
            },
            AuditRecordNotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Audit record not found",
                Detail = notFoundException.Message,
            },
            ExternalRateProviderException providerException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Currency provider unavailable",
                Detail = providerException.Message,
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Detail = "An unexpected error occurred.",
            },
        };

        return TypedResults.Problem(problemDetails);
    }
}

using Microsoft.AspNetCore.Http.HttpResults;
using OuterloopLabApi.Contracts;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Endpoints;

public static class CurrencyConversionEndpoints
{
    public static IEndpointRouteBuilder MapCurrencyConversionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/conversions");

        group.MapPost(string.Empty, HandleCreateConversionAsync)
            .WithName("CreateCurrencyConversion")
            .Produces<CurrencyConversionAuditResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{auditId}", HandleGetConversionAsync)
            .WithName("GetCurrencyConversionAudit")
            .Produces<CurrencyConversionAuditResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<Results<Ok<CurrencyConversionAuditResponse>, ProblemHttpResult>> HandleCreateConversionAsync(
        CreateCurrencyConversionRequest request,
        ICurrencyConversionService service,
        CancellationToken cancellationToken)
    {
        try
        {
            CurrencyConversionAuditResponse response = await service.CreateConversionAsync(request, cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (Exception exception) when (exception is DomainValidationException or ExternalRateProviderException)
        {
            return ExceptionMapping.ToProblemHttpResult(exception);
        }
    }

    public static async Task<Results<Ok<CurrencyConversionAuditResponse>, ProblemHttpResult>> HandleGetConversionAsync(
        string auditId,
        ICurrencyConversionService service,
        CancellationToken cancellationToken)
    {
        try
        {
            CurrencyConversionAuditResponse response = await service.GetConversionAsync(auditId, cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (Exception exception) when (exception is DomainValidationException or AuditRecordNotFoundException)
        {
            return ExceptionMapping.ToProblemHttpResult(exception);
        }
    }
}

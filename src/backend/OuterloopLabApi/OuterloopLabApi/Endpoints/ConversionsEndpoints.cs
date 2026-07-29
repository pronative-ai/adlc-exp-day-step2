using System.Net;
using OuterloopLabApi.Dtos;
using OuterloopLabApi.Repositories;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Endpoints;

public static class ConversionsEndpoints
{
  public static void Map(WebApplication app)
  {
    app.MapPost("/api/conversions/quote", async (ConversionQuoteRequestDto request, ConversionQuoteService service, CancellationToken ct) =>
    {
      if (request.Amount <= 0)
      {
        return Results.Problem(title: "Invalid request", detail: "Amount must be greater than 0.", statusCode: (int)HttpStatusCode.BadRequest);
      }

      var sourceCurrency = NormalizeCurrency(request.SourceCurrency);
      var targetCurrency = NormalizeCurrency(request.TargetCurrency);
      if (sourceCurrency is null || targetCurrency is null)
      {
        return Results.Problem(title: "Invalid request", detail: "Currency codes must be 3-letter ISO codes.", statusCode: (int)HttpStatusCode.BadRequest);
      }

      try
      {
        var quote = await service.QuoteAsync(request.Amount, sourceCurrency, targetCurrency, ct);
        return Results.Ok(quote);
      }
      catch (ExternalProviderException)
      {
        // Known Constraint: do not return raw exception messages.
        return Results.Problem(title: "Upstream provider failure", detail: "Unable to retrieve exchange rate at this time.", statusCode: (int)HttpStatusCode.ServiceUnavailable);
      }
      catch (ValidationException ex)
      {
        return Results.Problem(title: "Invalid request", detail: ex.Message, statusCode: (int)HttpStatusCode.BadRequest);
      }
      catch (Exception)
      {
        return Results.Problem(title: "Internal Server Error", detail: "An unexpected error occurred.", statusCode: (int)HttpStatusCode.InternalServerError);
      }
    });

    app.MapGet("/api/conversions/{id}", async (string id, IConversionAuditRepository repo, CancellationToken ct) =>
    {
      if (string.IsNullOrWhiteSpace(id))
      {
        return Results.Problem(title: "Invalid request", detail: "id is required.", statusCode: (int)HttpStatusCode.BadRequest);
      }

      try
      {
        var record = await repo.GetByIdAsync(id, ct);
        return record is null ? Results.Problem(title: "Not found", detail: "Conversion audit record not found.", statusCode: (int)HttpStatusCode.NotFound) : Results.Ok(record);
      }
      catch (Exception)
      {
        return Results.Problem(title: "Internal Server Error", detail: "An unexpected error occurred.", statusCode: (int)HttpStatusCode.InternalServerError);
      }
    });
  }

  private static string? NormalizeCurrency(string? code)
  {
    if (string.IsNullOrWhiteSpace(code)) return null;
    var normalized = code.Trim().ToUpperInvariant();
    return normalized.Length == 3 ? normalized : null;
  }
}

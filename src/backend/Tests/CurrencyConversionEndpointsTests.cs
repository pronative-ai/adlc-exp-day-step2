using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OuterloopLabApi.Contracts;
using OuterloopLabApi.Endpoints;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class CurrencyConversionEndpointsTests
{
    [Fact]
    public async Task HandleCreateConversionAsync_Returns503ProblemDetails_WhenProviderFails()
    {
        CreateCurrencyConversionRequest request = new()
        {
            Amount = 100.00m,
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
        };

        StubCurrencyConversionService service = new()
        {
            CreateException = new ExternalRateProviderException("Currency provider is currently unavailable."),
        };

        var result = await CurrencyConversionEndpoints.HandleCreateConversionAsync(request, service, CancellationToken.None);
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        string payload = await reader.ReadToEndAsync();
        using JsonDocument document = JsonDocument.Parse(payload);

        Assert.Equal("Currency provider unavailable", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(503, document.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class StubCurrencyConversionService : ICurrencyConversionService
    {
        public Exception? CreateException { get; init; }

        public Task<CurrencyConversionAuditResponse> CreateConversionAsync(CreateCurrencyConversionRequest request, CancellationToken cancellationToken)
        {
            throw CreateException ?? new InvalidOperationException("No response configured.");
        }

        public Task<CurrencyConversionAuditResponse> GetConversionAsync(string auditId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

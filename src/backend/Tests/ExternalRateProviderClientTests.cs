using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class ExternalRateProviderClientTests
{
    [Theory]
    [InlineData("{\"date\":\"2026-08-01\",\"rates\":{\"EUR\":0.92}}")]
    [InlineData("{\"date\":\"2026-08-01\",\"conversion_rates\":{\"EUR\":\"0.92\"},\"timestamp\":1722522738}")]
    public async Task GetQuoteAsync_NormalizesProviderPayloadShapes(string payload)
    {
        HttpClient httpClient = new(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload),
        }))
        {
            BaseAddress = new Uri("https://frankfurter.dev"),
        };

        ExternalRateProviderClient client = new(
            httpClient,
            Options.Create(new CurrencyApiOptions { BaseUrl = "https://frankfurter.dev" }),
            NullLogger<ExternalRateProviderClient>.Instance);

        var quote = await client.GetQuoteAsync("USD", "EUR", CancellationToken.None);

        Assert.Equal(0.92m, quote.Rate);
        Assert.Equal("USD", quote.SourceCurrency);
        Assert.Equal("EUR", quote.TargetCurrency);
        Assert.Equal("2026-08-01", quote.ProviderDate);
        Assert.Equal("https://frankfurter.dev", quote.ProviderBaseUrl);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}

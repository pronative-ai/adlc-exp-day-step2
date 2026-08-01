using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Services;
using Xunit;

namespace OuterloopLabApi.Tests;

public class FrankfurterRateProviderTests
{
    [Fact]
    public async Task GetRateAsync_OnSuccess_ReturnsRateAndProviderIdentity()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Response(HttpStatusCode.OK, """{"date":"2026-08-01","rates":{"EUR":0.9183}}"""));
        var provider = CreateProvider(handler);

        var rate = await provider.GetRateAsync("USD", "EUR", CancellationToken.None);

        Assert.Equal(0.9183m, rate.Rate);
        Assert.Equal("2026-08-01", rate.ProviderDate);
        Assert.Equal(FrankfurterRateProvider.ProviderName, rate.Provider);
    }

    [Fact]
    public async Task GetRateAsync_OnConversionRatesSchema_ExtractsRate()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Response(HttpStatusCode.OK, """{"base_code":"USD","conversion_rates":{"JPY":155.2}}"""));
        var provider = CreateProvider(handler);

        var rate = await provider.GetRateAsync("USD", "JPY", CancellationToken.None);

        Assert.Equal(155.2m, rate.Rate);
    }

    [Fact]
    public async Task GetRateAsync_WhenTargetCurrencyMissing_ThrowsInvalidConversion()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Response(HttpStatusCode.OK, """{"rates":{"EUR":0.9183}}"""));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidConversionException>(
            () => provider.GetRateAsync("USD", "GBP", CancellationToken.None));
    }

    [Fact]
    public async Task GetRateAsync_OnProvider4xx_ThrowsInvalidConversion()
    {
        var handler = new StubHttpMessageHandler(_ => Response(HttpStatusCode.BadRequest, "{}"));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidConversionException>(
            () => provider.GetRateAsync("ZZZ", "EUR", CancellationToken.None));
    }

    [Fact]
    public async Task GetRateAsync_OnProvider5xx_ThrowsRateProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => Response(HttpStatusCode.InternalServerError, "{}"));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<RateProviderUnavailableException>(
            () => provider.GetRateAsync("USD", "EUR", CancellationToken.None));
    }

    [Fact]
    public async Task GetRateAsync_OnNetworkError_ThrowsRateProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<RateProviderUnavailableException>(
            () => provider.GetRateAsync("USD", "EUR", CancellationToken.None));
    }

    private static FrankfurterRateProvider CreateProvider(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://frankfurter.dev")
        };
        return new FrankfurterRateProvider(httpClient, NullLogger<FrankfurterRateProvider>.Instance);
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string content)
        => new(status)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests;

public sealed class ConversionApiTests
{
  private sealed class StubRateClient : ICurrencyRateClient
  {
    private readonly CurrencyRate _rate;
    private readonly Exception? _toThrow;

    public StubRateClient(CurrencyRate rate)
    {
      _rate = rate;
      _toThrow = null;
    }

    public StubRateClient(Exception toThrow)
    {
      _toThrow = toThrow;
      _rate = null!;
    }

    public Task<CurrencyRate> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
    {
      if (_toThrow is not null) throw _toThrow;
      return Task.FromResult(_rate);
    }
  }

  private sealed class TestFactory : WebApplicationFactory<Program>
  {
    private readonly ICurrencyRateClient _rateClient;

    public TestFactory(ICurrencyRateClient rateClient)
    {
      _rateClient = rateClient;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      // Ensure Cosmos env vars are absent so the app uses in-memory persistence.
      Environment.SetEnvironmentVariable("COSMOS_DB_URI", null);
      Environment.SetEnvironmentVariable("COSMOS_DB_DATABASE", null);
      Environment.SetEnvironmentVariable("COSMOS_DB_CONTAINER", null);
      Environment.SetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID", null);

      builder.ConfigureServices(services =>
      {
        var toRemove = services.Where(d => d.ServiceType == typeof(ICurrencyRateClient)).ToList();
        foreach (var d in toRemove)
          services.Remove(d);
        services.AddSingleton(_rateClient);
      });
    }
  }

  [Fact]
  public async Task PostConversion_PersistsAndLookupReturnsSameRecord()
  {
    var rateClient = new StubRateClient(new CurrencyRate(
      Rate: 0.92m,
      BaseCurrency: "USD",
      TargetCurrency: "EUR",
      ProviderDateMarker: "2026-07-31",
      ProviderSequenceMarker: null));

    using var factory = new TestFactory(rateClient);
    var client = factory.CreateClient();

    var request = new ConversionRequest(Amount: 100.00m, FromCurrency: "USD", ToCurrency: "EUR");
    using var resp = await client.PostAsJsonAsync("/api/conversions", request);
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

    var created = await resp.Content.ReadFromJsonAsync<ConversionResultResponse>();
    Assert.NotNull(created);
    Assert.False(string.IsNullOrWhiteSpace(created!.AuditId));
    Assert.Equal(100.00m, created.Amount);
    Assert.Equal(0.92m, created.ExchangeRate);
    Assert.Equal(92.00m, created.ConvertedAmount);
    Assert.Equal("USD", created.FromCurrency);
    Assert.Equal("EUR", created.ToCurrency);
    Assert.Equal("2026-07-31", created.ProviderDateMarker);
    Assert.Null(created.ProviderSequenceMarker);
    Assert.Equal(DateTimeKind.Utc, created.ExecutionTimestampUtc.Kind);

    using var lookupResp = await client.GetAsync($"/api/conversions/{created.AuditId}");
    Assert.Equal(HttpStatusCode.OK, lookupResp.StatusCode);
    var lookup = await lookupResp.Content.ReadFromJsonAsync<ConversionResultResponse>();
    Assert.NotNull(lookup);
    Assert.Equal(created.AuditId, lookup!.AuditId);
    Assert.Equal(created.Amount, lookup.Amount);
    Assert.Equal(created.ExchangeRate, lookup.ExchangeRate);
    Assert.Equal(created.ConvertedAmount, lookup.ConvertedAmount);
    Assert.Equal(created.ExecutionTimestampUtc, lookup.ExecutionTimestampUtc);
  }

  [Fact]
  public async Task PostConversion_WhenProviderFails_Returns503ProblemDetails()
  {
    var rateClient = new StubRateClient(new CurrencyRateProviderUnavailableException("upstream down"));

    using var factory = new TestFactory(rateClient);
    var client = factory.CreateClient();

    var request = new ConversionRequest(Amount: 50m, FromCurrency: "USD", ToCurrency: "EUR");
    using var resp = await client.PostAsJsonAsync("/api/conversions", request);
    Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);

    var problem = await resp.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
    Assert.NotNull(problem);
    Assert.Equal(503, problem!.Status);
    Assert.NotNull(problem.Detail);
  }
}

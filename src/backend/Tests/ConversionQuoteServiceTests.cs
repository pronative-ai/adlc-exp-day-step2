using OuterloopLabApi.Dtos;
using OuterloopLabApi.Repositories;
using OuterloopLabApi.Services;
using Xunit;

namespace Tests;

public sealed class ConversionQuoteServiceTests
{
  [Fact]
  public async Task QuoteAsync_PersistsImmutableRecord()
  {
    var fakeRepo = new FakeRepo();
    var fakeClient = new FakeExchangeRateClient();
    var service = new ConversionQuoteService(fakeClient, fakeRepo);

    var record = await service.QuoteAsync(100.00m, "USD", "EUR", CancellationToken.None);

    Assert.NotEmpty(record.Id);
    Assert.Equal(100.00m, record.Amount);
    Assert.Equal("USD", record.SourceCurrency);
    Assert.Equal("EUR", record.TargetCurrency);
    Assert.Equal(0.92m, record.ExchangeRate);
    Assert.Equal(92.00m, record.ConvertedAmount);
    Assert.True(record.QuotedAtUtc <= DateTime.UtcNow);

    Assert.Single(fakeRepo.Created);
    Assert.Equal(record.Id, fakeRepo.Created[0].Id);
  }

  private sealed class FakeRepo : IConversionAuditRepository
  {
    public List<ConversionAuditRecordDto> Created { get; } = new();

    public Task CreateAsync(ConversionAuditRecordDto record, CancellationToken ct)
    {
      Created.Add(record);
      return Task.CompletedTask;
    }

    public Task<ConversionAuditRecordDto?> GetByIdAsync(string id, CancellationToken ct)
      => Task.FromResult<ConversionAuditRecordDto?>(null);
  }

  private sealed class FakeExchangeRateClient : IExchangeRateClient
  {
    public Task<ExchangeRateQuote> QuoteAsync(decimal amount, string sourceCurrency, string targetCurrency, CancellationToken ct)
      => Task.FromResult(new ExchangeRateQuote(0.92m, null, DateTime.UtcNow));
  }
}

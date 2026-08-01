using OuterloopLabApi.Contracts;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class CurrencyConversionServiceTests
{
    [Fact]
    public async Task CreateAndRetrieveConversionAsync_PreservesStoredAuditRecord()
    {
        FakeClock clock = new(new DateTimeOffset(2026, 8, 1, 14, 32, 18, 451, TimeSpan.Zero));
        InMemoryAuditRepository repository = new();
        StubRateProviderClient providerClient = new(new CurrencyQuote("USD", "EUR", 0.92m, "2026-08-01", null, "https://frankfurter.dev"));

        CurrencyConversionService service = new(
            providerClient,
            repository,
            clock);

        CurrencyConversionAuditResponse created = await service.CreateConversionAsync(
            new CreateCurrencyConversionRequest
            {
                Amount = 100.00m,
                SourceCurrency = "usd",
                TargetCurrency = "eur",
            },
            CancellationToken.None);

        providerClient.NextQuote = new CurrencyQuote("USD", "EUR", 0.50m, "2026-08-02", "changed", "https://frankfurter.dev");
        CurrencyConversionAuditResponse retrieved = await service.GetConversionAsync(created.AuditId, CancellationToken.None);

        Assert.Equal("USD", created.SourceCurrency);
        Assert.Equal("EUR", created.TargetCurrency);
        Assert.Equal(92.00m, created.ConvertedAmount);
        Assert.Equal(created.ConvertedAmount, retrieved.ConvertedAmount);
        Assert.Equal(created.Rate, retrieved.Rate);
        Assert.Equal(created.ExecutedAtUtc, retrieved.ExecutedAtUtc);
        Assert.Equal(created.ProviderDate, retrieved.ProviderDate);
        Assert.Equal(created.ProviderBaseUrl, retrieved.ProviderBaseUrl);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class StubRateProviderClient : IExternalRateProviderClient
    {
        public StubRateProviderClient(CurrencyQuote nextQuote)
        {
            NextQuote = nextQuote;
        }

        public CurrencyQuote NextQuote { get; set; }

        public Task<CurrencyQuote> GetQuoteAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken)
        {
            return Task.FromResult(NextQuote with
            {
                SourceCurrency = sourceCurrency,
                TargetCurrency = targetCurrency,
            });
        }
    }

    private sealed class InMemoryAuditRepository : ICurrencyConversionAuditRepository
    {
        private readonly Dictionary<string, CurrencyConversionAuditRecord> _records = new(StringComparer.Ordinal);

        public Task CreateAsync(CurrencyConversionAuditRecord record, CancellationToken cancellationToken)
        {
            _records[record.Id] = record;
            return Task.CompletedTask;
        }

        public Task<CurrencyConversionAuditRecord?> GetByIdAsync(string auditId, CancellationToken cancellationToken)
        {
            _records.TryGetValue(auditId, out CurrencyConversionAuditRecord? record);
            return Task.FromResult(record);
        }
    }
}

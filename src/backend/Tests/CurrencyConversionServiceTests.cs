using Microsoft.Extensions.Logging.Abstractions;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using Xunit;

namespace OuterloopLabApi.Tests;

public class CurrencyConversionServiceTests
{
    [Fact]
    public async Task ConvertAsync_WithLiveRate_ReturnsFreshResultAndPersistsAudit()
    {
        var provider = new FakeRateProvider(new ProviderRate(0.9183m, "2026-08-01", "frankfurter"));
        var audits = new FakeAuditRepository();
        var store = new InMemoryRateFallbackStore();
        var service = CreateService(provider, audits, store);

        var result = await service.ConvertAsync(
            new ConversionRequest { Amount = 1000m, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None);

        Assert.Equal(918.30m, result.ConvertedAmount);
        Assert.Equal(0.9183m, result.Rate);
        Assert.False(result.RateIsStale);
        Assert.Equal("frankfurter", result.Provider);
        Assert.Equal("2026-08-01", result.ProviderDate);
        Assert.NotEmpty(result.AuditId);
        Assert.True(result.ServerTimestamp.Ticks % TimeSpan.TicksPerSecond != 0, "Server timestamp must carry sub-second precision.");

        var record = Assert.Single(audits.Records);
        Assert.Equal(result.AuditId, record.Id);
        Assert.Equal("tenant-a", record.TenantId);
        Assert.False(record.RateIsStale);
        Assert.Equal(result.ServerTimestamp, record.ServerTimestamp);
    }

    [Fact]
    public async Task ConvertAsync_ProviderDownWithFallback_ReturnsStaleRateAndFlagsIt()
    {
        var store = new InMemoryRateFallbackStore();
        store.Remember("USD", "EUR", new ProviderRate(0.9m, "2026-08-01", "frankfurter"));

        var provider = new FakeRateProvider(new RateProviderUnavailableException("provider down", null));
        var audits = new FakeAuditRepository();
        var service = CreateService(provider, audits, store);

        var result = await service.ConvertAsync(
            new ConversionRequest { Amount = 100m, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None);

        Assert.Equal(0.9m, result.Rate);
        Assert.True(result.RateIsStale);
        Assert.Equal(90.00m, result.ConvertedAmount);

        var record = Assert.Single(audits.Records);
        Assert.True(record.RateIsStale);
        Assert.Equal(0.9m, record.Rate);
    }

    [Fact]
    public async Task ConvertAsync_ProviderDownWithoutFallback_ThrowsRateProviderUnavailable()
    {
        var provider = new FakeRateProvider(new RateProviderUnavailableException("provider down", null));
        var audits = new FakeAuditRepository();
        var service = CreateService(provider, audits, new InMemoryRateFallbackStore());

        await Assert.ThrowsAsync<RateProviderUnavailableException>(() =>
            service.ConvertAsync(
                new ConversionRequest { Amount = 100m, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None));

        Assert.Empty(audits.Records);
    }

    [Theory]
    [InlineData("US", "EUR")]
    [InlineData("USDD", "EUR")]
    [InlineData("USD", "")]
    public async Task ConvertAsync_WithFormatInvalidCurrency_ThrowsInvalidConversion(string from, string to)
    {
        var service = CreateService(
            new FakeRateProvider(new ProviderRate(1m, "2026-08-01", "frankfurter")),
            new FakeAuditRepository(),
            new InMemoryRateFallbackStore());

        await Assert.ThrowsAsync<InvalidConversionException>(() =>
            service.ConvertAsync(
                new ConversionRequest { Amount = 100m, From = from, To = to }, "tenant-a", CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ConvertAsync_WithNonPositiveAmount_ThrowsInvalidConversion(decimal amount)
    {
        var service = CreateService(
            new FakeRateProvider(new ProviderRate(1m, "2026-08-01", "frankfurter")),
            new FakeAuditRepository(),
            new InMemoryRateFallbackStore());

        await Assert.ThrowsAsync<InvalidConversionException>(() =>
            service.ConvertAsync(
                new ConversionRequest { Amount = amount, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None));
    }

    [Fact]
    public async Task ConvertAsync_RememberedRateUsedForLaterFallback()
    {
        var audits = new FakeAuditRepository();
        var store = new InMemoryRateFallbackStore();
        var provider = new FakeRateProvider(new ProviderRate(0.9183m, "2026-08-01", "frankfurter"));
        var service = CreateService(provider, audits, store);

        await service.ConvertAsync(
            new ConversionRequest { Amount = 100m, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None);

        provider.Next = new RateProviderUnavailableException("provider down", null);

        var result = await service.ConvertAsync(
            new ConversionRequest { Amount = 100m, From = "USD", To = "EUR" }, "tenant-a", CancellationToken.None);

        Assert.True(result.RateIsStale);
        Assert.Equal(0.9183m, result.Rate);
    }

    private static CurrencyConversionService CreateService(
        FakeRateProvider provider,
        FakeAuditRepository audits,
        IRateFallbackStore store)
        => new(provider, audits, store, NullLogger<CurrencyConversionService>.Instance);

    private sealed class FakeRateProvider : ICurrencyRateProvider
    {
        public FakeRateProvider(ProviderRate rate) => _rate = rate;

        public FakeRateProvider(RateProviderUnavailableException exception) => _exception = exception;

        private readonly ProviderRate? _rate;
        private readonly RateProviderUnavailableException? _exception;

        public Exception? Next { get; set; }

        public Task<ProviderRate> GetRateAsync(string from, string to, CancellationToken cancellationToken)
        {
            if (Next is RateProviderUnavailableException ex)
            {
                throw ex;
            }

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_rate!);
        }
    }

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public List<AuditRecord> Records { get; } = new();

        public Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<AuditRecord?> GetAsync(string tenantId, string auditId, CancellationToken cancellationToken)
            => Task.FromResult(Records.FirstOrDefault(r => r.Id == auditId && r.TenantId == tenantId));
    }
}

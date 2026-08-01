using OuterloopLabApi.Audits;
using OuterloopLabApi.Conversion;
using OuterloopLabApi.Models;
using OuterloopLabApi.Providers;
using Xunit;

public class ConversionServiceTests
{
    private sealed class FakeProvider : ICurrencyRateProvider
    {
        public NormalizedProviderResult Result { get; init; } = new(2.0m, "2026-08-01", null, "{}");

        public Task<NormalizedProviderResult> GetNormalizedRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeRepo : IAuditRepository
    {
        public AuditRecord? Last { get; private set; }

        public Task AddAsync(AuditRecord record, CancellationToken cancellationToken)
        {
            Last = record;
            return Task.CompletedTask;
        }

        public Task<AuditRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AuditRecord?>(Last);
        }

        public Task<IReadOnlyList<AuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AuditRecord>>(new[] { Last! });
        }
    }

    [Fact]
    public async Task ConvertAsync_PersistsAuditAndReturnsConvertedAmount()
    {
        var repo = new FakeRepo();
        var provider = new FakeProvider { Result = new NormalizedProviderResult(2.0m, "2026-08-01", null, "{}"); };
        var svc = new ConversionService(provider, repo);

        var req = new ConvertRequest { Amount = 10m, FromCurrency = "usd", ToCurrency = "eur" };
        var resp = await svc.ConvertAsync(req, CancellationToken.None);

        Assert.NotNull(repo.Last);
        Assert.Equal("usd", repo.Last!.PartitionKey);
        Assert.Equal(20.0m, repo.Last.ConvertedAmount);
        Assert.Equal(resp.ConvertedAmount, repo.Last.ConvertedAmount);
        Assert.Equal("2026-08-01", resp.ProviderDate);
    }

    [Fact]
    public async Task ConvertAsync_RejectsNonPositiveAmount()
    {
        var svc = new ConversionService(new FakeProvider(), new FakeRepo());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ConvertAsync(new ConvertRequest
        {
            Amount = 0m,
            FromCurrency = "USD",
            ToCurrency = "EUR"
        }, CancellationToken.None));
    }
}

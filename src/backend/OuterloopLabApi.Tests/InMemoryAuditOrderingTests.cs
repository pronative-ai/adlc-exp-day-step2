using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class InMemoryAuditOrderingTests
{
    private sealed class InMemoryRepo : IConversionAuditRepository
    {
        private readonly List<ConversionAuditEntity> _items = new();

        public Task<string> AddAsync(ConversionAuditEntity entity, CancellationToken ct)
        {
            _items.Add(entity);
            return Task.FromResult(entity.Id);
        }

        public Task<IReadOnlyList<ConversionAuditEntity>> GetRecentAsync(int limit, CancellationToken ct)
        {
            var results = _items
                .OrderByDescending(x => x.ExecutedAtUtc)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<ConversionAuditEntity>>(results);
        }
    }

    [Fact]
    public async Task GetRecent_Returns_Reverse_Chronological_Ordering()
    {
        var repo = new InMemoryRepo();
        await repo.AddAsync(new ConversionAuditEntity
        {
            Id = "1",
            Pk = "all",
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            OriginalAmount = 10,
            ConversionRate = 0.9m,
            ConvertedAmount = 9,
            ProviderDateMarker = "2026-08-01",
            ProviderSequenceMarker = null,
            ExecutedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 10, 0, 0), DateTimeKind.Utc),
        }, CancellationToken.None);

        await repo.AddAsync(new ConversionAuditEntity
        {
            Id = "2",
            Pk = "all",
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            OriginalAmount = 20,
            ConversionRate = 1.1m,
            ConvertedAmount = 22,
            ProviderDateMarker = "2026-08-01",
            ProviderSequenceMarker = null,
            ExecutedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 10, 0, 1), DateTimeKind.Utc),
        }, CancellationToken.None);

        var items = await repo.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(new[] { "2", "1" }, items.Select(x => x.Id).ToArray());
    }
}

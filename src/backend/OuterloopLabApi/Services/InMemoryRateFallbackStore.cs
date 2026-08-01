using System.Collections.Concurrent;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class InMemoryRateFallbackStore : IRateFallbackStore
{
    private readonly ConcurrentDictionary<(string From, string To), ProviderRate> _rates = new();

    public void Remember(string from, string to, ProviderRate rate)
        => _rates[(Normalize(from), Normalize(to))] = rate;

    public bool TryGet(string from, string to, out ProviderRate rate)
        => _rates.TryGetValue((Normalize(from), Normalize(to)), out rate!);

    private static string Normalize(string currency) => currency.ToUpperInvariant();
}

using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using Xunit;

namespace OuterloopLabApi.Tests;

public class InMemoryRateFallbackStoreTests
{
    [Fact]
    public void RememberAndTryGet_StoresRatePerCurrencyPair()
    {
        var store = new InMemoryRateFallbackStore();
        store.Remember("USD", "EUR", new ProviderRate(0.9183m, "2026-08-01", "frankfurter"));

        Assert.True(store.TryGet("USD", "EUR", out var rate));
        Assert.Equal(0.9183m, rate.Rate);
        Assert.Equal("2026-08-01", rate.ProviderDate);
    }

    [Fact]
    public void Remember_IsCaseInsensitiveForCurrencyCodes()
    {
        var store = new InMemoryRateFallbackStore();
        store.Remember("usd", "eur", new ProviderRate(0.9m, "2026-08-01", "frankfurter"));

        Assert.True(store.TryGet("USD", "EUR", out _));
    }

    [Fact]
    public void TryGet_WhenPairNeverSeen_ReturnsFalse()
    {
        var store = new InMemoryRateFallbackStore();

        Assert.False(store.TryGet("USD", "JPY", out _));
    }

    [Fact]
    public void Remember_OverwritesPreviousRateForSamePair()
    {
        var store = new InMemoryRateFallbackStore();
        store.Remember("USD", "EUR", new ProviderRate(0.9m, "2026-08-01", "frankfurter"));
        store.Remember("USD", "EUR", new ProviderRate(0.85m, "2026-08-02", "frankfurter"));

        Assert.True(store.TryGet("USD", "EUR", out var rate));
        Assert.Equal(0.85m, rate.Rate);
    }
}

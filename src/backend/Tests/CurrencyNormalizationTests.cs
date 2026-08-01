using System.Text.Json;
using OuterloopLabApi.Providers;
using Xunit;

public class CurrencyNormalizationTests
{
    [Fact]
    public void TryNormalize_UsesRates_WhenPresent()
    {
        var json = "{\"date\":\"2026-08-01\",\"rates\":{\"EUR\":0.92}}";
        using var doc = JsonDocument.Parse(json);

        var ok = ProviderNormalization.TryNormalize(doc, "EUR", out var normalized);
        Assert.True(ok);
        Assert.Equal(0.92m, normalized.Rate);
        Assert.Equal("2026-08-01", normalized.ProviderDate);
        Assert.Contains("\"rates\"", normalized.ProviderRawJson);
    }

    [Fact]
    public void TryNormalize_UsesConversionRates_WhenPresent()
    {
        var json = "{\"provider_date\":\"2026-08-01\",\"conversion_rates\":{\"EUR\":\"0.91\"}}";
        using var doc = JsonDocument.Parse(json);

        var ok = ProviderNormalization.TryNormalize(doc, "EUR", out var normalized);
        Assert.True(ok);
        Assert.Equal(0.91m, normalized.Rate);
        Assert.Equal("2026-08-01", normalized.ProviderDate);
    }

    [Fact]
    public void TryNormalize_ReadsSequenceMarker()
    {
        var json = "{\"seq\":12,\"rates\":{\"EUR\":0.88}}";
        using var doc = JsonDocument.Parse(json);

        var ok = ProviderNormalization.TryNormalize(doc, "EUR", out var normalized);
        Assert.True(ok);
        Assert.Equal("12", normalized.ProviderSequenceMarker);
    }

    [Fact]
    public void TryNormalize_ReturnsFalse_WhenNoRatesOrConversionRates()
    {
        var json = "{\"date\":\"2026-08-01\"}";
        using var doc = JsonDocument.Parse(json);

        var ok = ProviderNormalization.TryNormalize(doc, "EUR", out _);
        Assert.False(ok);
    }
}

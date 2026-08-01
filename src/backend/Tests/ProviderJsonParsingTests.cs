using OuterloopLabApi.External;
using Xunit;

namespace Tests;

public sealed class ProviderJsonParsingTests
{
    [Fact]
    public void ParsesRate_FromRatesProperty()
    {
        var json = "{\"base\":\"USD\",\"date\":\"2026-08-01\",\"rates\":{\"EUR\":0.92}}";
        var res = FrankfurterCurrencyConversionProvider.ParseProviderJson(json, "EUR");
        Assert.Equal(0.92m, res.Rate);
        Assert.Equal("USD", res.ProviderBaseCurrency);
        Assert.Equal("2026-08-01", res.ProviderDate);
    }

    [Fact]
    public void ParsesRate_FromConversionRatesProperty()
    {
        var json = "{\"base\":\"USD\",\"timestamp\":\"2026-08-01T10:20:30Z\",\"conversion_rates\":{\"EUR\":1.23}}";
        var res = FrankfurterCurrencyConversionProvider.ParseProviderJson(json, "EUR");
        Assert.Equal(1.23m, res.Rate);
        Assert.Equal("USD", res.ProviderBaseCurrency);
        Assert.Equal("2026-08-01T10:20:30Z", res.ProviderDate);
    }
}

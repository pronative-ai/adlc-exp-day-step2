using OuterloopLabApi.Services;
using Xunit;

namespace OuterloopLabApi.Tests;

public class ProviderRateParserTests
{
    [Fact]
    public void TryParse_WithRatesProperty_ExtractsRateAndDate()
    {
        const string json = """{"amount":1.0,"base":"USD","date":"2026-08-01","rates":{"EUR":0.9183}}""";

        var result = ProviderRateParser.TryParse(json, "EUR");

        Assert.NotNull(result);
        Assert.Equal(0.9183m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderDate);
    }

    [Fact]
    public void TryParse_WithConversionRatesProperty_ExtractsRate()
    {
        const string json = """{"base_code":"USD","conversion_rates":{"INR":84.5}}""";

        var result = ProviderRateParser.TryParse(json, "INR");

        Assert.NotNull(result);
        Assert.Equal(84.5m, result.Rate);
        Assert.Null(result.ProviderDate);
    }

    [Fact]
    public void TryParse_WithoutRatesOrConversionRates_ReturnsNull()
    {
        const string json = """{"base":"USD","quotes":{"EUR":0.9}}""";

        var result = ProviderRateParser.TryParse(json, "EUR");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_WhenTargetCurrencyMissing_ReturnsNull()
    {
        const string json = """{"date":"2026-08-01","rates":{"EUR":0.9183}}""";

        var result = ProviderRateParser.TryParse(json, "GBP");

        Assert.Null(result);
    }

    [Fact]
    public void TryParse_PrefersConversionRatesOverRates_WhenBothPresent()
    {
        const string json = """{"rates":{"EUR":0.5},"conversion_rates":{"EUR":0.9183}}""";

        var result = ProviderRateParser.TryParse(json, "EUR");

        Assert.NotNull(result);
        Assert.Equal(0.9183m, result.Rate);
    }
}

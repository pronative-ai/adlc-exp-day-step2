using System;
using Xunit;

public class RateParsingTests
{
    [Theory]
    [InlineData("{\"rate\":0.92,\"date\":\"2026-08-01\"}", 0.92, "2026-08-01", null)]
    [InlineData("{\"rates\":{\"EUR\":0.92},\"date\":\"2026-08-01\"}", 0.92, "2026-08-01", null)]
    [InlineData("{\"conversion_rates\":{\"EUR\":0.92},\"date\":\"2026-08-01\"}", 0.92, "2026-08-01", null)]
    public void Adapter_Extracts_Rate_From_Varied_Schemas(string payload, double expectedRate, string expectedDate, string? expectedSequence)
    {
        var adapter = new ExternalCurrencyRateProviderAdapter();
        var result = adapter.Parse(payload, "USD", "EUR");

        Assert.Equal((decimal)expectedRate, result.Rate);
        Assert.Equal(expectedDate, result.ProviderDateMarker);
        Assert.Equal(expectedSequence, result.ProviderSequenceMarker);
    }

    [Fact]
    public void Adapter_Extracts_Sequence_When_Present()
    {
        var adapter = new ExternalCurrencyRateProviderAdapter();
        var payload = "{\"rate\":0.92,\"date\":\"2026-08-01\",\"sequence\":\"seq-1\"}";
        var result = adapter.Parse(payload, "USD", "EUR");

        Assert.Equal((decimal)0.92, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderDateMarker);
        Assert.Equal("seq-1", result.ProviderSequenceMarker);
    }
}

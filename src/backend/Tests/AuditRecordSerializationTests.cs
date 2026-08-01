using System.Text.Json;
using OuterloopLabApi.Json;
using OuterloopLabApi.Models;
using Xunit;

namespace OuterloopLabApi.Tests;

public class AuditRecordSerializationTests
{
    [Fact]
    public void Serialize_ProducesExpectedPropertyNames()
    {
        var record = new AuditRecord
        {
            TenantId = "tenant-a",
            Amount = 1000m,
            FromCurrency = "USD",
            ToCurrency = "EUR",
            Rate = 0.9183m,
            Provider = "frankfurter",
            ProviderDate = "2026-08-01",
            ServerTimestamp = new DateTimeOffset(2026, 8, 1, 9, 15, 32, TimeSpan.Zero).AddTicks(1234567),
            RateIsStale = false,
        };

        var json = JsonSerializer.Serialize(record, Options);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("id", out _));
        Assert.Equal("tenant-a", root.GetProperty("tenantId").GetString());
        Assert.Equal(1000m, root.GetProperty("amount").GetDecimal());
        Assert.Equal("USD", root.GetProperty("fromCurrency").GetString());
        Assert.Equal("EUR", root.GetProperty("toCurrency").GetString());
        Assert.Equal(0.9183m, root.GetProperty("rate").GetDecimal());
        Assert.Equal("frankfurter", root.GetProperty("provider").GetString());
        Assert.Equal("2026-08-01", root.GetProperty("providerDate").GetString());
        Assert.Equal("2026-08-01T09:15:32.1234567Z", root.GetProperty("serverTimestamp").GetString());
        Assert.False(root.GetProperty("rateIsStale").GetBoolean());
    }

    [Fact]
    public void ConversionResult_SerializesWithCamelCaseNames()
    {
        var result = new ConversionResult
        {
            Amount = 1000m,
            From = "USD",
            To = "EUR",
            ConvertedAmount = 918.30m,
            Rate = 0.9183m,
            Provider = "frankfurter",
            ProviderDate = "2026-08-01",
            ServerTimestamp = new DateTimeOffset(2026, 8, 1, 9, 15, 32, TimeSpan.Zero).AddTicks(1234567),
            RateIsStale = false,
            AuditId = "abc-123",
        };

        var json = JsonSerializer.Serialize(result, Options);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1000m, root.GetProperty("amount").GetDecimal());
        Assert.Equal("USD", root.GetProperty("from").GetString());
        Assert.Equal("EUR", root.GetProperty("to").GetString());
        Assert.Equal(918.30m, root.GetProperty("convertedAmount").GetDecimal());
        Assert.Equal(0.9183m, root.GetProperty("rate").GetDecimal());
        Assert.Equal("2026-08-01", root.GetProperty("providerDate").GetString());
        Assert.Equal("2026-08-01T09:15:32.1234567Z", root.GetProperty("serverTimestamp").GetString());
        Assert.False(root.GetProperty("rateIsStale").GetBoolean());
        Assert.Equal("abc-123", root.GetProperty("auditId").GetString());
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new UtcDateTimeOffsetConverter() }
    };
}

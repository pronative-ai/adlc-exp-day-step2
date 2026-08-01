using System.Text.Json;
using OuterloopLabApi.Json;
using Xunit;

namespace OuterloopLabApi.Tests;

public class UtcDateTimeOffsetConverterTests
{
    [Fact]
    public void Write_UsesUtcWithSevenFractionalDigitsAndZ()
    {
        var timestamp = new DateTimeOffset(2026, 8, 1, 9, 15, 32, TimeSpan.Zero)
            .AddTicks(1234567);

        var json = JsonSerializer.Serialize(timestamp, Options);

        Assert.Equal("\"2026-08-01T09:15:32.1234567Z\"", json);
    }

    [Fact]
    public void Read_AcceptsUtcFormat()
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-08-01T09:15:32.1234567Z\"", Options);

        Assert.Equal(2026, value.Year);
        Assert.Equal(1234567, value.Ticks % TimeSpan.TicksPerSecond);
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeOffsetConverter() }
    };
}

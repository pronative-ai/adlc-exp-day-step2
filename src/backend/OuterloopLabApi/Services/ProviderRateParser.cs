using System.Text.Json;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

/// <summary>
/// Parses third-party rate provider responses into a neutral internal model using
/// flexible property mapping so provider schema changes (e.g. <c>rates</c> vs
/// <c>conversion_rates</c>) cannot break internal compliance tracking.
/// </summary>
public static class ProviderRateParser
{
    public static ProviderRateData? TryParse(string json, string currency)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("conversion_rates", out var rates) &&
            !root.TryGetProperty("rates", out rates))
        {
            return null;
        }

        if (rates.ValueKind != JsonValueKind.Object ||
            !rates.TryGetProperty(currency, out var rateElement) ||
            !rateElement.TryGetDecimal(out var rate))
        {
            return null;
        }

        var providerDate = root.TryGetProperty("date", out var dateElement) &&
                           dateElement.ValueKind == JsonValueKind.String
            ? dateElement.GetString()
            : null;

        return new ProviderRateData(rate, providerDate);
    }
}

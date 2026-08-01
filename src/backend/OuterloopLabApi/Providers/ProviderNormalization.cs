using System.Globalization;
using System.Text.Json;

namespace OuterloopLabApi.Providers;

public sealed record NormalizedProviderResult(
    decimal Rate,
    string? ProviderDate,
    string? ProviderSequenceMarker,
    string ProviderRawJson);

public static class ProviderNormalization
{
    public static bool TryNormalize(
        JsonDocument doc,
        string requestedToCurrency,
        out NormalizedProviderResult result)
    {
        result = default!;

        var toCurrency = requestedToCurrency.Trim().ToUpperInvariant();
        var root = doc.RootElement;

        string? providerDate = null;
        if (root.TryGetProperty("date", out var dateEl) && dateEl.ValueKind == JsonValueKind.String)
            providerDate = dateEl.GetString();
        if (providerDate is null && root.TryGetProperty("provider_date", out var pd) && pd.ValueKind == JsonValueKind.String)
            providerDate = pd.GetString();

        string? providerSequenceMarker = null;
        if (TryGetStringOrNumber(root, "sequence", out var seq))
            providerSequenceMarker = seq;
        else if (TryGetStringOrNumber(root, "seq", out var seq2))
            providerSequenceMarker = seq2;
        else if (TryGetStringOrNumber(root, "provider_sequence", out var seq3))
            providerSequenceMarker = seq3;

        JsonElement ratesEl;
        if (root.TryGetProperty("rates", out var ratesCandidate))
        {
            ratesEl = ratesCandidate;
        }
        else if (root.TryGetProperty("conversion_rates", out var cr))
        {
            ratesEl = cr;
        }
        else
        {
            return false;
        }

        if (ratesEl.ValueKind != JsonValueKind.Object)
            return false;

        if (!ratesEl.TryGetProperty(toCurrency, out var rateEl))
        {
            // Some providers may return a single rate without an explicit currency key.
            // If so, accept the first numeric value.
            foreach (var prop in ratesEl.EnumerateObject())
            {
                if (TryReadDecimal(prop.Value, out var d))
                {
                    result = new NormalizedProviderResult(
                        d,
                        providerDate,
                        providerSequenceMarker,
                        root.GetRawText());
                    return true;
                }
            }

            return false;
        }

        if (!TryReadDecimal(rateEl, out var rate))
            return false;

        result = new NormalizedProviderResult(
            rate,
            providerDate,
            providerSequenceMarker,
            root.GetRawText());
        return true;
    }

    private static bool TryGetStringOrNumber(JsonElement root, string propertyName, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var el))
            return false;

        if (el.ValueKind == JsonValueKind.String)
        {
            value = el.GetString();
            return value is not null;
        }

        if (el.ValueKind == JsonValueKind.Number)
        {
            if (el.TryGetDecimal(out var d))
            {
                value = d.ToString(CultureInfo.InvariantCulture);
                return true;
            }
        }

        return false;
    }

    private static bool TryReadDecimal(JsonElement el, out decimal value)
    {
        value = 0m;

        if (el.ValueKind == JsonValueKind.Number)
            return el.TryGetDecimal(out value);

        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return false;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        return false;
    }
}

using System.Globalization;
using System.Text.Json;

namespace OuterloopLabApi.External;

public sealed class FrankfurterCurrencyConversionProvider : ICurrencyConversionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ConversionProviderOptions _options;

    public FrankfurterCurrencyConversionProvider(HttpClient httpClient, ConversionProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<ProviderConversionResult> GetLatestRateAsync(string sourceCurrency, string targetCurrency)
    {
        // Constraint: only use base URL from environment variable (with default configured in Program.cs).
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var requestUri = $"{baseUrl}/latest?base={Uri.EscapeDataString(sourceCurrency)}&symbols={Uri.EscapeDataString(targetCurrency)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri);
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderUnavailableException("Failed to contact conversion provider.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CurrencyProviderUnavailableException("Conversion provider returned an error status.");
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            return ParseProviderJson(json, targetCurrency);
        }
        catch (CurrencyProviderUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderUnavailableException("Conversion provider payload could not be parsed.", ex);
        }
    }

    public static ProviderConversionResult ParseProviderJson(string json, string targetCurrency)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Flexible mapping: rates vs conversion_rates.
        decimal? rate = null;
        if (TryGetRateFromObject(root, "rates", targetCurrency, out var r1)) rate = r1;
        if (rate is null && TryGetRateFromObject(root, "conversion_rates", targetCurrency, out var r2)) rate = r2;

        if (rate is null)
        {
            throw new CurrencyProviderUnavailableException("Conversion rate not found in provider payload.");
        }

        string? providerDate = TryGetString(root, new[] { "date", "timestamp", "time" });
        string? providerSequence = TryGetString(root, new[] { "sequence", "seq" });
        string? providerBaseCurrency = TryGetString(root, new[] { "base" });

        return new ProviderConversionResult(rate.Value, providerBaseCurrency, providerDate, providerSequence);
    }

    private static bool TryGetRateFromObject(JsonElement root, string propertyName, string targetCurrency, out decimal rate)
    {
        rate = 0m;
        if (!root.TryGetProperty(propertyName, out var ratesEl) || ratesEl.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!ratesEl.TryGetProperty(targetCurrency, out var targetRateEl))
        {
            return false;
        }

        // Provider numbers might come as JSON numbers.
        if (targetRateEl.ValueKind == JsonValueKind.Number)
        {
            if (targetRateEl.TryGetDecimal(out var d))
            {
                rate = d;
                return true;
            }
        }

        if (targetRateEl.ValueKind == JsonValueKind.String)
        {
            var s = targetRateEl.GetString();
            if (s is not null && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dd))
            {
                rate = dd;
                return true;
            }
        }

        return false;
    }

    private static string? TryGetString(JsonElement root, string[] candidates)
    {
        foreach (var c in candidates)
        {
            if (root.TryGetProperty(c, out var el))
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    return el.GetString();
                }
                if (el.ValueKind == JsonValueKind.Number)
                {
                    return el.GetRawText();
                }
            }
        }

        return null;
    }
}

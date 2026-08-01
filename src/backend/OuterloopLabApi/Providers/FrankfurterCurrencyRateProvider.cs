using System.Net.Http.Json;
using System.Text.Json;

namespace OuterloopLabApi.Providers;

public sealed class FrankfurterCurrencyRateProvider : ICurrencyRateProvider
{
    private readonly HttpClient _httpClient;

    public FrankfurterCurrencyRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<NormalizedProviderResult> GetNormalizedRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        // Using Frankfurter-compatible contract by default. Response schema normalization is handled separately.
        var requestUri = $"/latest?from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}";

        HttpResponseMessage resp;
        try
        {
            resp = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderException("Currency provider request failed.", ex);
        }

        if (!resp.IsSuccessStatusCode)
            throw new CurrencyProviderException($"Currency provider returned HTTP {(int)resp.StatusCode}.");

        var jsonText = await resp.Content.ReadAsStringAsync(cancellationToken);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonText);
        }
        catch (Exception ex)
        {
            throw new CurrencyProviderException("Currency provider returned invalid JSON.", ex);
        }

        if (!ProviderNormalization.TryNormalize(doc, toCurrency, out var normalized))
            throw new CurrencyProviderException("Currency provider payload could not be normalized.");

        return normalized;
    }
}

using System.Text.Json;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class CurrencyRateClient : ICurrencyRateClient
{
  private readonly HttpClient _httpClient;

  public CurrencyRateClient(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<CurrencyRate> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default)
  {
    try
    {
      // Frankfurter-like providers typically accept /latest?from={}&to={}
      // but response schemas may differ; parsing is normalized.
      var baseUrl = _httpClient.BaseAddress?.ToString() ?? string.Empty;
      var url = $"latest?from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}";

      var res = await _httpClient.GetAsync(url, cancellationToken);
      if (!res.IsSuccessStatusCode)
        throw new CurrencyRateProviderUnavailableException("Provider returned non-success status.");

      var content = await res.Content.ReadAsStringAsync(cancellationToken);

      using var doc = JsonDocument.Parse(content);
      var root = doc.RootElement;

      var ratesObj = TryGetObject(root, "rates") ?? TryGetObject(root, "conversion_rates");
      if (ratesObj is null)
        throw new CurrencyRateParseException("Provider payload did not contain rates.");

      var targetKey = FindCaseInsensitiveProperty(ratesObj.Value, toCurrency);
      if (targetKey is null)
        throw new CurrencyRateParseException("Provider payload did not contain target currency rate.");

      var rateValue = ParseDecimalFromJson(ratesObj.Value.GetProperty(targetKey));
      var providerDate = TryGetString(root, "date") ?? TryGetString(root, "provider_date");
      var providerSeq = TryGetString(root, "sequence") ?? TryGetString(root, "seq") ?? TryGetString(root, "provider_sequence");
      var providerBaseCurrency = TryGetString(root, "base") ?? fromCurrency;

      return new CurrencyRate(rateValue, providerBaseCurrency, toCurrency, providerDate, providerSeq);
    }
    catch (CurrencyRateProviderUnavailableException)
    {
      throw;
    }
    catch (JsonException ex)
    {
      throw new CurrencyRateParseException("Provider payload could not be parsed as JSON.", ex);
    }
    catch (TaskCanceledException ex)
    {
      throw new CurrencyRateProviderUnavailableException("Provider call timed out.", ex);
    }
    catch (CurrencyRateParseException)
    {
      throw;
    }
    catch (Exception ex)
    {
      throw new CurrencyRateProviderUnavailableException("Provider call failed.", ex);
    }
  }

  private static JsonElement? TryGetObject(JsonElement root, string propertyName)
  {
    if (root.ValueKind != JsonValueKind.Object) return null;
    if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Object)
      return element;
    return null;
  }

  private static string? TryGetString(JsonElement root, string propertyName)
  {
    if (root.ValueKind != JsonValueKind.Object) return null;
    if (!root.TryGetProperty(propertyName, out var element)) return null;
    if (element.ValueKind == JsonValueKind.String) return element.GetString();
    if (element.ValueKind == JsonValueKind.Number) return element.ToString();
    return null;
  }

  private static string? FindCaseInsensitiveProperty(JsonElement obj, string desired)
  {
    foreach (var prop in obj.EnumerateObject())
    {
      if (string.Equals(prop.Name, desired, StringComparison.OrdinalIgnoreCase)) return prop.Name;
    }
    return null;
  }

  private static decimal ParseDecimalFromJson(JsonElement element)
  {
    return element.ValueKind switch
    {
      JsonValueKind.Number => element.GetDecimal(),
      JsonValueKind.String => decimal.Parse(element.GetString() ?? "0"),
      _ => throw new CurrencyRateParseException("Rate value is not a number.")
    };
  }
}

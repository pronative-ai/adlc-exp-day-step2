using System.Net;
using System.Text.Json;
using OuterloopLabApi.Options;
using Microsoft.Extensions.Options;

namespace OuterloopLabApi.Services;

public interface IExchangeRateClient
{
  Task<ExchangeRateQuote> QuoteAsync(decimal amount, string sourceCurrency, string targetCurrency, CancellationToken ct);
}

public sealed class ExchangeRateClient : IExchangeRateClient
{
  private readonly HttpClient _httpClient;
  private readonly ProviderOptions _options;

  public ExchangeRateClient(HttpClient httpClient, IOptions<ProviderOptions> options)
  {
    _httpClient = httpClient;
    _options = options.Value;
    _httpClient.Timeout = TimeSpan.FromSeconds(10);
  }

  public async Task<ExchangeRateQuote> QuoteAsync(decimal amount, string sourceCurrency, string targetCurrency, CancellationToken ct)
  {
    // Provider is external; we treat failures as upstream failures and surface as 503.
    var amountString = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Example request: {baseUrl}?from=USD&to=EUR&amount=100
    var url = $"{_options.ProviderBaseUrl}?from={Uri.EscapeDataString(sourceCurrency)}&to={Uri.EscapeDataString(targetCurrency)}&amount={Uri.EscapeDataString(amountString)}";

    using var resp = await _httpClient.GetAsync(url, ct);
    if (!resp.IsSuccessStatusCode)
    {
      throw new ExternalProviderException($"Provider returned HTTP {(int)resp.StatusCode}.");
    }

    var payload = await resp.Content.ReadAsStringAsync(ct);
    using var doc = JsonDocument.Parse(payload);

    // exchangerate.host convert returns: {"success":true,"query":{"from":"USD"...},"result": 1.23, ...}
    if (doc.RootElement.TryGetProperty("result", out var resultEl) && resultEl.TryGetDecimal(out var resultDecimal))
    {
      var exchangeRate = resultDecimal / amount;

      DateTime quotedAtUtc = DateTime.UtcNow;
      if (doc.RootElement.TryGetProperty("date", out var dateEl) && dateEl.ValueKind == JsonValueKind.String)
      {
        // date is usually YYYY-MM-DD
        if (DateTime.TryParse(dateEl.GetString(), out var dt))
          quotedAtUtc = dt.ToUniversalTime();
      }

      return new ExchangeRateQuote(exchangeRate, ProviderResponseId: null, QuotedAtUtc: quotedAtUtc);
    }

    throw new ExternalProviderException("Provider response did not contain a valid exchange rate result.");
  }
}

public sealed record ExchangeRateQuote(decimal ExchangeRate, string? ProviderResponseId, DateTime QuotedAtUtc);

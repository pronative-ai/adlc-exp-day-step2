using System.Net;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface IExternalRateProviderClient
{
    Task<CurrencyQuote> GetQuoteAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken);
}

public sealed class ExternalRateProviderClient : IExternalRateProviderClient
{
    private static readonly string[] RatePropertyCandidates = ["rates", "conversion_rates"];
    private static readonly string[] ProviderDateCandidates = ["date", "provider_date", "last_updated_at"];
    private static readonly string[] ProviderSequenceCandidates = ["sequence", "sequence_number", "timestamp", "time_last_update_unix", "time_last_update_utc"];

    private readonly HttpClient _httpClient;
    private readonly CurrencyApiOptions _currencyApiOptions;
    private readonly ILogger<ExternalRateProviderClient> _logger;

    public ExternalRateProviderClient(
        HttpClient httpClient,
        IOptions<CurrencyApiOptions> currencyApiOptions,
        ILogger<ExternalRateProviderClient> logger)
    {
        _httpClient = httpClient;
        _currencyApiOptions = currencyApiOptions.Value;
        _logger = logger;
    }

    public async Task<CurrencyQuote> GetQuoteAsync(string sourceCurrency, string targetCurrency, CancellationToken cancellationToken)
    {
        string requestUri = $"/v1/latest?base={Uri.EscapeDataString(sourceCurrency)}&symbols={Uri.EscapeDataString(targetCurrency)}";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Currency provider returned HTTP {StatusCode} for {SourceCurrency}/{TargetCurrency}.", (int)response.StatusCode, sourceCurrency, targetCurrency);
                throw new ExternalRateProviderException(BuildStatusMessage(response.StatusCode));
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            JsonElement root = document.RootElement;
            if (!TryGetExchangeRate(root, targetCurrency, out decimal rate))
            {
                throw new ExternalRateProviderException("Currency provider response could not be normalized.");
            }

            return new CurrencyQuote(
                sourceCurrency,
                targetCurrency,
                rate,
                GetOptionalString(root, ProviderDateCandidates),
                GetOptionalString(root, ProviderSequenceCandidates),
                _currencyApiOptions.BaseUrl);
        }
        catch (ExternalRateProviderException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Currency provider request failed for {SourceCurrency}/{TargetCurrency}.", sourceCurrency, targetCurrency);
            throw new ExternalRateProviderException("Currency provider is currently unavailable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Currency provider request timed out for {SourceCurrency}/{TargetCurrency}.", sourceCurrency, targetCurrency);
            throw new ExternalRateProviderException("Currency provider request timed out.", exception);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Currency provider response was not valid JSON for {SourceCurrency}/{TargetCurrency}.", sourceCurrency, targetCurrency);
            throw new ExternalRateProviderException("Currency provider response could not be normalized.", exception);
        }
    }

    private static bool TryGetExchangeRate(JsonElement root, string targetCurrency, out decimal rate)
    {
        foreach (string propertyName in RatePropertyCandidates)
        {
            if (TryGetPropertyIgnoreCase(root, propertyName, out JsonElement rateContainer) &&
                rateContainer.ValueKind == JsonValueKind.Object &&
                TryGetPropertyIgnoreCase(rateContainer, targetCurrency, out JsonElement rateElement) &&
                TryReadDecimal(rateElement, out rate))
            {
                return true;
            }
        }

        rate = default;
        return false;
    }

    private static string? GetOptionalString(JsonElement root, IReadOnlyCollection<string> propertyCandidates)
    {
        foreach (string propertyName in propertyCandidates)
        {
            if (TryGetPropertyIgnoreCase(root, propertyName, out JsonElement value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.ToString(),
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    _ => value.ToString(),
                };
            }
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryReadDecimal(JsonElement value, out decimal result)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.TryGetDecimal(out result);
        }

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static string BuildStatusMessage(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.NotFound => "Currency provider endpoint was not found.",
            HttpStatusCode.TooManyRequests => "Currency provider is rate limiting requests.",
            _ => "Currency provider is currently unavailable.",
        };
}

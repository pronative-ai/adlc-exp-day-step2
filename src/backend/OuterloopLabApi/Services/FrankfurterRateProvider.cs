using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class FrankfurterRateProvider : ICurrencyRateProvider
{
    public const string ProviderName = "frankfurter";

    private const string LatestPath = "/latest";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterRateProvider> _logger;

    public FrankfurterRateProvider(HttpClient httpClient, ILogger<FrankfurterRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProviderRate> GetRateAsync(string from, string to, CancellationToken cancellationToken)
    {
        var requestUri = $"{LatestPath}?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RateProviderUnavailableException("The currency rate provider timed out.", null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RateProviderUnavailableException("The currency rate provider is unreachable.", ex);
        }

        string content;
        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RateProviderUnavailableException("The currency rate provider returned an unreadable response.", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            var data = ProviderRateParser.TryParse(content, to);
            if (data is null)
            {
                _logger.LogWarning("Rate provider response for {From}/{To} did not contain a usable rate.", from, to);
                throw new InvalidConversionException($"Currency code '{to}' is not supported by the rate provider.");
            }

            return new ProviderRate(data.Rate, data.ProviderDate, ProviderName);
        }

        if ((int)response.StatusCode is >= 400 and < 500)
        {
            throw new InvalidConversionException($"The rate provider rejected the currency pair '{from}' to '{to}'.");
        }

        throw new RateProviderUnavailableException(
            $"The currency rate provider returned status {(int)response.StatusCode}.", null);
    }
}

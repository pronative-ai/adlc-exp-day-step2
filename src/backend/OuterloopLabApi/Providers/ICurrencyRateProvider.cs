using System.Text.Json;

namespace OuterloopLabApi.Providers;

public interface ICurrencyRateProvider
{
    Task<NormalizedProviderResult> GetNormalizedRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken);
}

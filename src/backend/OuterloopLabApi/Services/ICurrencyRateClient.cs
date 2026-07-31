using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface ICurrencyRateClient
{
  Task<CurrencyRate> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken = default);
}

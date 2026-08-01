using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface ICurrencyRateProvider
{
    Task<ProviderRate> GetRateAsync(string from, string to, CancellationToken cancellationToken);
}

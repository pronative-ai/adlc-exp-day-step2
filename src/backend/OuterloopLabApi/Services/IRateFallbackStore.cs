using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface IRateFallbackStore
{
    void Remember(string from, string to, ProviderRate rate);

    bool TryGet(string from, string to, out ProviderRate rate);
}

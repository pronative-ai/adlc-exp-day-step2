using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class AuditRecord
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString("D");

    [JsonPropertyName("tenantId")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("fromCurrency")]
    public string FromCurrency { get; init; } = string.Empty;

    [JsonPropertyName("toCurrency")]
    public string ToCurrency { get; init; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; init; }

    [JsonPropertyName("provider")]
    public string Provider { get; init; } = string.Empty;

    [JsonPropertyName("providerDate")]
    public string? ProviderDate { get; init; }

    [JsonPropertyName("serverTimestamp")]
    public DateTimeOffset ServerTimestamp { get; init; }

    [JsonPropertyName("rateIsStale")]
    public bool RateIsStale { get; init; }
}

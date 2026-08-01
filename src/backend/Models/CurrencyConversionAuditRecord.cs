using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed record CurrencyConversionAuditRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("originalAmount")]
    public decimal OriginalAmount { get; init; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; init; }

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; init; }

    [JsonPropertyName("providerDate")]
    public string? ProviderDate { get; init; }

    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; init; }

    [JsonPropertyName("providerBaseUrl")]
    public required string ProviderBaseUrl { get; init; }

    [JsonPropertyName("executedAtUtc")]
    public DateTimeOffset ExecutedAtUtc { get; init; }
}

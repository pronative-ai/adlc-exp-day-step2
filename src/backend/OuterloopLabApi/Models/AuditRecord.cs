using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class AuditRecord
{
    // Cosmos item id (string) - also returned to frontend for auditor lookups.
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Cosmos partition key. We use it to support fast writes.
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    // Request markers.
    [JsonPropertyName("fromCurrency")]
    public string FromCurrency { get; set; } = string.Empty;

    [JsonPropertyName("toCurrency")]
    public string ToCurrency { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    // Normalized conversion artifacts.
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; set; }

    // Backend execution timestamp (UTC) for sub-second audit reconstruction.
    [JsonPropertyName("executionTimestampUtc")]
    public DateTime ExecutionTimestampUtc { get; set; }

    // Provider markers (may be calendar-only).
    [JsonPropertyName("providerDate")]
    public string? ProviderDate { get; set; }

    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; set; }

    // Stored as raw JSON text for reconstructability without trusting provider schema.
    [JsonPropertyName("providerRawJson")]
    public string ProviderRawJson { get; set; } = string.Empty;
}

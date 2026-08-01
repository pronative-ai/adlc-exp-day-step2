using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class ConvertResponse
{
    [JsonPropertyName("auditId")]
    public string AuditId { get; init; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; init; }

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; init; }

    [JsonPropertyName("executionTimestampUtc")]
    public DateTime ExecutionTimestampUtc { get; init; }

    [JsonPropertyName("providerDate")]
    public string? ProviderDate { get; init; }

    [JsonPropertyName("providerSequenceMarker")]
    public string? ProviderSequenceMarker { get; init; }
}

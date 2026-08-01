using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class ConvertRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("fromCurrency")]
    public string FromCurrency { get; init; } = string.Empty;

    [JsonPropertyName("toCurrency")]
    public string ToCurrency { get; init; } = string.Empty;
}

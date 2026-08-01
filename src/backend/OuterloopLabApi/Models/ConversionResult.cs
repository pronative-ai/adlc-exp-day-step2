namespace OuterloopLabApi.Models;

public sealed class ConversionResult
{
    public decimal Amount { get; init; }

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public decimal ConvertedAmount { get; init; }

    public decimal Rate { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string? ProviderDate { get; init; }

    public DateTimeOffset ServerTimestamp { get; init; }

    public bool RateIsStale { get; init; }

    public string AuditId { get; init; } = string.Empty;
}

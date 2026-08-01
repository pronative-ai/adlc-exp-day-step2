using OuterloopLabApi.Audits;
using OuterloopLabApi.Models;
using OuterloopLabApi.Providers;

namespace OuterloopLabApi.Conversion;

public sealed class ConversionService
{
    private readonly ICurrencyRateProvider _provider;
    private readonly IAuditRepository _auditRepository;

    public ConversionService(ICurrencyRateProvider provider, IAuditRepository auditRepository)
    {
        _provider = provider;
        _auditRepository = auditRepository;
    }

    public async Task<ConvertResponse> ConvertAsync(ConvertRequest request, CancellationToken cancellationToken)
    {
        var executionTimestampUtc = DateTime.UtcNow;

        var from = request.FromCurrency.Trim().ToUpperInvariant();
        var to = request.ToCurrency.Trim().ToUpperInvariant();

        if (request.Amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        var normalized = await _provider.GetNormalizedRateAsync(from, to, cancellationToken);

        var convertedAmount = request.Amount * normalized.Rate;

        var audit = new AuditRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            PartitionKey = from,
            FromCurrency = from,
            ToCurrency = to,
            Amount = request.Amount,
            Rate = normalized.Rate,
            ConvertedAmount = convertedAmount,
            ExecutionTimestampUtc = executionTimestampUtc,
            ProviderDate = normalized.ProviderDate,
            ProviderSequenceMarker = normalized.ProviderSequenceMarker,
            ProviderRawJson = normalized.ProviderRawJson,
        };

        await _auditRepository.AddAsync(audit, cancellationToken);

        return new ConvertResponse
        {
            AuditId = audit.Id,
            Rate = audit.Rate,
            ConvertedAmount = audit.ConvertedAmount,
            ExecutionTimestampUtc = audit.ExecutionTimestampUtc,
            ProviderDate = audit.ProviderDate,
            ProviderSequenceMarker = audit.ProviderSequenceMarker,
        };
    }
}

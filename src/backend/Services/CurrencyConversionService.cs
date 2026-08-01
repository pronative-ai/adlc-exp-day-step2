using OuterloopLabApi.Contracts;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface ICurrencyConversionService
{
    Task<CurrencyConversionAuditResponse> CreateConversionAsync(CreateCurrencyConversionRequest request, CancellationToken cancellationToken);

    Task<CurrencyConversionAuditResponse> GetConversionAsync(string auditId, CancellationToken cancellationToken);
}

public sealed class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IExternalRateProviderClient _externalRateProviderClient;
    private readonly ICurrencyConversionAuditRepository _auditRepository;
    private readonly IClock _clock;

    public CurrencyConversionService(
        IExternalRateProviderClient externalRateProviderClient,
        ICurrencyConversionAuditRepository auditRepository,
        IClock clock)
    {
        _externalRateProviderClient = externalRateProviderClient;
        _auditRepository = auditRepository;
        _clock = clock;
    }

    public async Task<CurrencyConversionAuditResponse> CreateConversionAsync(CreateCurrencyConversionRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new DomainValidationException("Amount must be greater than zero.");
        }

        string sourceCurrency = NormalizeCurrencyCode(request.SourceCurrency, nameof(request.SourceCurrency));
        string targetCurrency = NormalizeCurrencyCode(request.TargetCurrency, nameof(request.TargetCurrency));

        CurrencyQuote quote = await _externalRateProviderClient.GetQuoteAsync(sourceCurrency, targetCurrency, cancellationToken);
        DateTimeOffset executedAtUtc = _clock.UtcNow;
        decimal convertedAmount = decimal.Round(request.Amount * quote.Rate, 2, MidpointRounding.AwayFromZero);

        CurrencyConversionAuditRecord record = new()
        {
            Id = Guid.NewGuid().ToString(),
            SourceCurrency = sourceCurrency,
            TargetCurrency = targetCurrency,
            OriginalAmount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Rate = quote.Rate,
            ConvertedAmount = convertedAmount,
            ProviderDate = quote.ProviderDate,
            ProviderSequenceMarker = quote.ProviderSequenceMarker,
            ProviderBaseUrl = quote.ProviderBaseUrl,
            ExecutedAtUtc = executedAtUtc,
        };

        await _auditRepository.CreateAsync(record, cancellationToken);
        return MapToResponse(record);
    }

    public async Task<CurrencyConversionAuditResponse> GetConversionAsync(string auditId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auditId))
        {
            throw new DomainValidationException("Audit id is required.");
        }

        CurrencyConversionAuditRecord? record = await _auditRepository.GetByIdAsync(auditId.Trim(), cancellationToken);
        if (record is null)
        {
            throw new AuditRecordNotFoundException(auditId.Trim());
        }

        return MapToResponse(record);
    }

    private static string NormalizeCurrencyCode(string currencyCode, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        string normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(static character => !char.IsAsciiLetter(character)))
        {
            throw new DomainValidationException($"{fieldName} must be a 3-letter ISO currency code.");
        }

        return normalized;
    }

    private static CurrencyConversionAuditResponse MapToResponse(CurrencyConversionAuditRecord record) =>
        new(
            record.Id,
            record.SourceCurrency,
            record.TargetCurrency,
            record.OriginalAmount,
            record.Rate,
            record.ConvertedAmount,
            record.ProviderDate,
            record.ProviderSequenceMarker,
            record.ProviderBaseUrl,
            record.ExecutedAtUtc);
}

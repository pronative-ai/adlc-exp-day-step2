using System.Text.RegularExpressions;
using OuterloopLabApi.Exceptions;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class CurrencyConversionService
{
    private static readonly Regex CurrencyCodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled);

    private readonly ICurrencyRateProvider _rateProvider;
    private readonly IAuditRepository _auditRepository;
    private readonly IRateFallbackStore _fallbackStore;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(
        ICurrencyRateProvider rateProvider,
        IAuditRepository auditRepository,
        IRateFallbackStore fallbackStore,
        ILogger<CurrencyConversionService> logger)
    {
        _rateProvider = rateProvider;
        _auditRepository = auditRepository;
        _fallbackStore = fallbackStore;
        _logger = logger;
    }

    public async Task<ConversionResult> ConvertAsync(
        ConversionRequest request,
        string tenantId,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var from = request.From.Trim().ToUpperInvariant();
        var to = request.To.Trim().ToUpperInvariant();

        var resolvedRate = await FetchRateAsync(from, to, cancellationToken);

        var convertedAmount = Math.Round(request.Amount * resolvedRate.Rate, 2, MidpointRounding.AwayFromZero);
        var serverTimestamp = DateTimeOffset.UtcNow;

        var record = new AuditRecord
        {
            TenantId = tenantId,
            Amount = request.Amount,
            FromCurrency = from,
            ToCurrency = to,
            Rate = resolvedRate.Rate,
            Provider = resolvedRate.Provider,
            ProviderDate = resolvedRate.ProviderDate,
            ServerTimestamp = serverTimestamp,
            RateIsStale = resolvedRate.RateIsStale,
        };

        await _auditRepository.AddAsync(record, cancellationToken);

        return new ConversionResult
        {
            Amount = request.Amount,
            From = from,
            To = to,
            ConvertedAmount = convertedAmount,
            Rate = resolvedRate.Rate,
            Provider = resolvedRate.Provider,
            ProviderDate = resolvedRate.ProviderDate,
            ServerTimestamp = serverTimestamp,
            RateIsStale = resolvedRate.RateIsStale,
            AuditId = record.Id,
        };
    }

    public async Task<AuditRecord?> GetAuditAsync(string tenantId, string auditId, CancellationToken cancellationToken)
        => await _auditRepository.GetAsync(tenantId, auditId, cancellationToken);

    private async Task<ResolvedRate> FetchRateAsync(string from, string to, CancellationToken cancellationToken)
    {
        try
        {
            var providerRate = await _rateProvider.GetRateAsync(from, to, cancellationToken);
            _fallbackStore.Remember(from, to, providerRate);
            return new ResolvedRate(providerRate.Rate, providerRate.ProviderDate, providerRate.Provider, RateIsStale: false);
        }
        catch (RateProviderUnavailableException ex)
        {
            _logger.LogWarning("Rate provider unavailable for {From}/{To}; attempting fallback. {Message}", from, to, ex.Message);
            if (_fallbackStore.TryGet(from, to, out var fallback))
            {
                return new ResolvedRate(fallback.Rate, fallback.ProviderDate, fallback.Provider, RateIsStale: true);
            }

            throw;
        }
    }

    private static void ValidateRequest(ConversionRequest request)
    {
        if (request is null)
        {
            throw new InvalidConversionException("A conversion request body is required.");
        }

        if (!CurrencyCodePattern.IsMatch(request.From?.Trim() ?? string.Empty))
        {
            throw new InvalidConversionException($"'{request.From}' is not a valid 3-letter ISO 4217 currency code.");
        }

        if (!CurrencyCodePattern.IsMatch(request.To?.Trim() ?? string.Empty))
        {
            throw new InvalidConversionException($"'{request.To}' is not a valid 3-letter ISO 4217 currency code.");
        }

        if (request.Amount <= 0)
        {
            throw new InvalidConversionException("Amount must be greater than zero.");
        }
    }

    private sealed record ResolvedRate(decimal Rate, string? ProviderDate, string Provider, bool RateIsStale);
}

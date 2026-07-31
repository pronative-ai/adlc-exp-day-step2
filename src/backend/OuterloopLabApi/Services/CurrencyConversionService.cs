using OuterloopLabApi.Models;
using OuterloopLabApi.Repositories;

namespace OuterloopLabApi.Services;

public sealed class CurrencyConversionService
{
  private readonly ICurrencyRateClient _rateClient;
  private readonly IAuditTrailRepository _auditRepo;
  private readonly string _providerBaseUrl;

  public CurrencyConversionService(ICurrencyRateClient rateClient, IAuditTrailRepository auditRepo)
  {
    _rateClient = rateClient;
    _auditRepo = auditRepo;
    _providerBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL") ?? "https://frankfurter.dev";
  }

  public async Task<ConversionResultResponse> ConvertAsync(ConversionRequest request, CancellationToken cancellationToken = default)
  {
    var rate = await _rateClient.GetRateAsync(request.FromCurrency, request.ToCurrency, cancellationToken);
    var convertedAmount = request.Amount * rate.Rate;

    var auditId = Guid.NewGuid().ToString("N");
    var executionTimestampUtc = DateTime.UtcNow;

    var record = new AuditTrailRecord
    {
      AuditId = auditId,
      Amount = request.Amount,
      FromCurrency = request.FromCurrency,
      ToCurrency = request.ToCurrency,
      ExchangeRate = rate.Rate,
      ConvertedAmount = convertedAmount,
      ExecutionTimestampUtc = executionTimestampUtc,
      ProviderBaseUrl = _providerBaseUrl,
      ProviderDateMarker = rate.ProviderDateMarker,
      ProviderSequenceMarker = rate.ProviderSequenceMarker
    };

    await _auditRepo.CreateAsync(record, cancellationToken);

    return new ConversionResultResponse(
      auditId,
      record.Amount,
      record.FromCurrency,
      record.ToCurrency,
      record.ExchangeRate,
      record.ConvertedAmount,
      record.ExecutionTimestampUtc,
      record.ProviderDateMarker,
      record.ProviderSequenceMarker);
  }

  public async Task<ConversionResultResponse?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken = default)
  {
    var record = await _auditRepo.GetByAuditIdAsync(auditId, cancellationToken);
    if (record is null) return null;

    return new ConversionResultResponse(
      record.AuditId,
      record.Amount,
      record.FromCurrency,
      record.ToCurrency,
      record.ExchangeRate,
      record.ConvertedAmount,
      record.ExecutionTimestampUtc,
      record.ProviderDateMarker,
      record.ProviderSequenceMarker);
  }
}

using System.Globalization;
using OuterloopLabApi.Dtos;
using OuterloopLabApi.Repositories;

namespace OuterloopLabApi.Services;

public sealed class ConversionQuoteService
{
  private readonly IExchangeRateClient _exchangeRateClient;
  private readonly IConversionAuditRepository _auditRepository;

  public ConversionQuoteService(IExchangeRateClient exchangeRateClient, IConversionAuditRepository auditRepository)
  {
    _exchangeRateClient = exchangeRateClient;
    _auditRepository = auditRepository;
  }

  public async Task<ConversionAuditRecordDto> QuoteAsync(decimal amount, string sourceCurrency, string targetCurrency, CancellationToken ct)
  {
    // Normalize to avoid decimal precision mismatches.
    amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    var quote = await _exchangeRateClient.QuoteAsync(amount, sourceCurrency, targetCurrency, ct);
    var exchangeRate = Math.Round(quote.ExchangeRate, 6, MidpointRounding.AwayFromZero);
    var convertedAmount = Math.Round(amount * exchangeRate, 2, MidpointRounding.AwayFromZero);

    var record = new ConversionAuditRecordDto
    {
      Id = Guid.NewGuid().ToString(),
      Amount = amount,
      SourceCurrency = sourceCurrency,
      TargetCurrency = targetCurrency,
      ExchangeRate = exchangeRate,
      ConvertedAmount = convertedAmount,
      ProviderResponseId = quote.ProviderResponseId,
      QuotedAtUtc = quote.QuotedAtUtc
    };

    // Do not create audit record on failure; this call only happens after provider success.
    await _auditRepository.CreateAsync(record, ct);
    return record;
  }
}

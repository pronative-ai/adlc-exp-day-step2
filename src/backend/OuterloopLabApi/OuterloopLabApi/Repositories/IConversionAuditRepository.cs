using OuterloopLabApi.Dtos;

namespace OuterloopLabApi.Repositories;

public interface IConversionAuditRepository
{
  Task<ConversionAuditRecordDto?> GetByIdAsync(string id, CancellationToken ct);
  Task CreateAsync(ConversionAuditRecordDto record, CancellationToken ct);
}

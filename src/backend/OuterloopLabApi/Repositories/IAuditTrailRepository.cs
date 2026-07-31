using OuterloopLabApi.Models;

namespace OuterloopLabApi.Repositories;

public interface IAuditTrailRepository
{
  Task CreateAsync(AuditTrailRecord record, CancellationToken cancellationToken = default);
  Task<AuditTrailRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken = default);
}

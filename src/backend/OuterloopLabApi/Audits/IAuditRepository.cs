using OuterloopLabApi.Models;

namespace OuterloopLabApi.Audits;

public interface IAuditRepository
{
    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);
    Task<AuditRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken);
}

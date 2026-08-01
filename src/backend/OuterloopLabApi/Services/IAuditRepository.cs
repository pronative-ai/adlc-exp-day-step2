using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface IAuditRepository
{
    Task AddAsync(AuditRecord record, CancellationToken cancellationToken);

    Task<AuditRecord?> GetAsync(string tenantId, string auditId, CancellationToken cancellationToken);
}

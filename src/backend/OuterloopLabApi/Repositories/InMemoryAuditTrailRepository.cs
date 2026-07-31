using System.Collections.Concurrent;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Repositories;

public sealed class InMemoryAuditTrailRepository : IAuditTrailRepository
{
  private readonly ConcurrentDictionary<string, AuditTrailRecord> _store = new();

  public Task CreateAsync(AuditTrailRecord record, CancellationToken cancellationToken = default)
  {
    _store[record.AuditId] = record;
    return Task.CompletedTask;
  }

  public Task<AuditTrailRecord?> GetByAuditIdAsync(string auditId, CancellationToken cancellationToken = default)
  {
    _store.TryGetValue(auditId, out var record);
    return Task.FromResult(record);
  }
}

import { apiUrl } from './client';
import type { ConvertResponse } from './types';

export async function convert(req: {
  amount: number;
  fromCurrency: string;
  toCurrency: string;
}): Promise<ConvertResponse> {
  const r = await fetch(apiUrl('/api/convert'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!r.ok) {
    const problem = await r.json().catch(() => null);
    const detail = problem?.detail ?? `HTTP ${r.status}`;
    throw new Error(detail);
  }
  return (await r.json()) as ConvertResponse;
}

export async function getRecentAudits(limit: number): Promise<ConvertResponse[]> {
  const r = await fetch(apiUrl(`/api/audits?limit=${encodeURIComponent(String(limit))}`));
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return (await r.json()) as ConvertResponse[];
}

export async function getAuditById(auditId: string): Promise<ConvertResponse> {
  const r = await fetch(apiUrl(`/api/audits/${encodeURIComponent(auditId)}`));
  if (r.status === 404) throw new Error('Audit record not found.');
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return (await r.json()) as ConvertResponse;
}

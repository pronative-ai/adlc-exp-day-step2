import { getRuntimeApiBaseUrl } from './runtimeApiUrl'
import type {
  CreateConversionRequest,
  CreateConversionResponse,
  ConversionAuditRecord,
} from './types'

function withApiUrl(path: string): string {
  const base = getRuntimeApiBaseUrl().replace(/\/$/, '')
  if (!base) return path
  return `${base}${path}`
}

export async function postConversion(req: CreateConversionRequest): Promise<CreateConversionResponse> {
  const response = await fetch(withApiUrl('/api/conversions'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  })

  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`Conversion failed (${response.status}). ${text}`.trim())
  }

  return (await response.json()) as CreateConversionResponse
}

export async function getConversions(limit: number): Promise<ConversionAuditRecord[]> {
  const response = await fetch(withApiUrl(`/api/conversions?limit=${encodeURIComponent(String(limit))}`))
  if (!response.ok) {
    const text = await response.text().catch(() => '')
    throw new Error(`Failed to load history (${response.status}). ${text}`.trim())
  }

  const body = (await response.json()) as { items: ConversionAuditRecord[] }
  return body.items
}

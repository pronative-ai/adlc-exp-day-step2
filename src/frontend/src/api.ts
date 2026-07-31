export type ConversionRequest = {
  amount: number
  fromCurrency: string
  toCurrency: string
}

export type ConversionResult = {
  auditId: string
  amount: number
  fromCurrency: string
  toCurrency: string
  exchangeRate: number
  convertedAmount: number
  executionTimestampUtc: string
  providerDateMarker?: string | null
  providerSequenceMarker?: string | null
}

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

function resolveApiBaseUrl(): string {
  // This value is injected at container runtime by replacing __VITE_API_URL__ in index.html.
  const val = (window as any).__VITE_API_URL__
  return typeof val === 'string' ? val : ''
}

export async function createConversion(request: ConversionRequest): Promise<ConversionResult> {
  const baseUrl = resolveApiBaseUrl()
  const res = await fetch(`${baseUrl}/api/conversions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!res.ok) {
    const text = await res.text().catch(() => '')
    let parsed: ProblemDetails | undefined
    try {
      parsed = text ? (JSON.parse(text) as ProblemDetails) : undefined
    } catch {
      parsed = undefined
    }
    const detail = parsed?.detail ?? text ?? `Request failed with ${res.status}`
    throw new Error(detail)
  }

  return (await res.json()) as ConversionResult
}

export async function getConversion(auditId: string): Promise<ConversionResult> {
  const baseUrl = resolveApiBaseUrl()
  const res = await fetch(`${baseUrl}/api/conversions/${encodeURIComponent(auditId)}`)

  if (!res.ok) {
    const text = await res.text().catch(() => '')
    let parsed: ProblemDetails | undefined
    try {
      parsed = text ? (JSON.parse(text) as ProblemDetails) : undefined
    } catch {
      parsed = undefined
    }
    const detail = parsed?.detail ?? text ?? `Request failed with ${res.status}`
    throw new Error(detail)
  }

  return (await res.json()) as ConversionResult
}

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
}

function getApiBaseUrl() {
  // Populated by the container runtime entrypoint placeholder replacement in index.html.
  return window.__VITE_API_URL__ ?? ''
}

export async function createConversion(input: {
  sourceCurrency: string
  targetCurrency: string
  amount: number
}): Promise<any> {
  const base = getApiBaseUrl()
  const res = await fetch(`${base}/api/conversions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input)
  })
  if (!res.ok) {
    const problem = (await res.json().catch(() => null)) as ProblemDetails | null
    throw problem ?? { title: 'Request failed', status: res.status }
  }
  return res.json()
}

export async function getConversion(id: string): Promise<any> {
  const base = getApiBaseUrl()
  const res = await fetch(`${base}/api/conversions/${encodeURIComponent(id)}`, {
    method: 'GET'
  })
  if (!res.ok) {
    const problem = (await res.json().catch(() => null)) as ProblemDetails | null
    throw problem ?? { title: 'Request failed', status: res.status }
  }
  return res.json()
}

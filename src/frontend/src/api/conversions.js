function getApiBaseUrl() {
  const runtime = window.__RUNTIME_CONFIG__
  const viteApiUrl = runtime?.VITE_API_URL
  return typeof viteApiUrl === 'string' ? viteApiUrl : ''
}

function normalizeError(problem) {
  if (!problem) return 'Request failed.'
  if (typeof problem.detail === 'string' && problem.detail.length > 0) return problem.detail
  if (typeof problem.title === 'string' && problem.title.length > 0) return problem.title
  return 'Request failed.'
}

export async function quoteConversion({ amount, sourceCurrency, targetCurrency }) {
  const apiBase = getApiBaseUrl()
  const url = `${apiBase}/api/conversions/quote`

  const resp = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ amount, sourceCurrency, targetCurrency })
  })

  const data = await resp.json().catch(() => null)
  if (!resp.ok) {
    throw new Error(normalizeError(data))
  }
  return data
}

export async function fetchConversionById(id) {
  const apiBase = getApiBaseUrl()
  const url = `${apiBase}/api/conversions/${encodeURIComponent(id)}`

  const resp = await fetch(url, {
    method: 'GET'
  })

  const data = await resp.json().catch(() => null)
  if (!resp.ok) {
    throw new Error(normalizeError(data))
  }
  return data
}

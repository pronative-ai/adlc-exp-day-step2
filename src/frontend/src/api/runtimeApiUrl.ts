export function getRuntimeApiBaseUrl(): string {
  const globalValue = (globalThis as unknown as { __VITE_API_URL__?: string }).__VITE_API_URL__
  const meta = document.querySelector('meta[name="vite-api-url"]')
  const metaValue = meta?.getAttribute('content') ?? undefined

  const value = globalValue ?? metaValue ?? ''
  if (value === '__VITE_API_URL__') return ''
  return value
}

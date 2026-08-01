import { describe, expect, it, vi } from 'vitest'
import { getRuntimeApiBaseUrl } from './runtimeApiUrl'

function setMeta(content: string) {
  const existing = document.querySelector('meta[name="vite-api-url"]')
  if (existing) existing.remove()
  const meta = document.createElement('meta')
  meta.setAttribute('name', 'vite-api-url')
  meta.setAttribute('content', content)
  document.head.appendChild(meta)
}

describe('getRuntimeApiBaseUrl', () => {
  it('reads from window.__VITE_API_URL__ when present', () => {
    ;(globalThis as any).__VITE_API_URL__ = 'http://example.test'
    setMeta('__VITE_API_URL__')
    expect(getRuntimeApiBaseUrl()).toBe('http://example.test')
  })

  it('falls back to meta tag content', () => {
    delete (globalThis as any).__VITE_API_URL__
    setMeta('http://meta.test')
    expect(getRuntimeApiBaseUrl()).toBe('http://meta.test')
  })

  it('treats placeholder token as empty', () => {
    ;(globalThis as any).__VITE_API_URL__ = '__VITE_API_URL__'
    setMeta('__VITE_API_URL__')
    expect(getRuntimeApiBaseUrl()).toBe('')
  })
})

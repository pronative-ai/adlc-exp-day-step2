import { describe, it, expect, vi, beforeEach } from 'vitest'
import { quoteConversion } from './conversions'

describe('conversions API', () => {
  beforeEach(() => {
    vi.stubGlobal('__VITE_API_URL__', '')
    global.window = {
      __RUNTIME_CONFIG__: { VITE_API_URL: '' }
    }
  })

  it('throws a user-safe error when the server returns a ProblemDetails payload', async () => {
    vi.stubGlobal('fetch', async () => {
      return {
        ok: false,
        status: 503,
        json: async () => ({ title: 'Upstream provider failure', detail: 'Unable to retrieve exchange rate at this time.' })
      }
    })

    await expect(
      quoteConversion({ amount: 10, sourceCurrency: 'USD', targetCurrency: 'EUR' })
    ).rejects.toThrow('Unable to retrieve exchange rate at this time.')
  })
})

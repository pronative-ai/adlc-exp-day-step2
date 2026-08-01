import React, { useMemo, useState } from 'react'
import { postConversion } from '../api/client'
import type { CreateConversionResponse, CurrencyCode } from '../api/types'

const currencies = [
  'USD',
  'EUR',
  'GBP',
  'JPY',
  'CHF',
  'AUD',
  'CAD',
  'INR',
]

function normalizeCurrency(code: string): CurrencyCode {
  return code.trim().toUpperCase()
}

export default function ConversionPage() {
  const [amountText, setAmountText] = useState('')
  const [sourceCurrency, setSourceCurrency] = useState<CurrencyCode>('USD')
  const [targetCurrency, setTargetCurrency] = useState<CurrencyCode>('EUR')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CreateConversionResponse | null>(null)

  const amount = useMemo(() => {
    const parsed = Number(amountText)
    if (!Number.isFinite(parsed)) return null
    return parsed
  }, [amountText])

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setResult(null)

    if (amount === null || amount <= 0) {
      setError('Amount must be a positive number.')
      return
    }

    setSubmitting(true)
    try {
      const response = await postConversion({
        amount,
        sourceCurrency,
        targetCurrency,
      })
      setResult(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Conversion failed')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 16 }}>
      <form onSubmit={onSubmit} style={{ border: '1px solid #e5e5e5', padding: 16, borderRadius: 12 }}>
        <h2 style={{ marginTop: 0, fontSize: 16 }}>New Conversion</h2>
        <div style={{ display: 'grid', gap: 12, gridTemplateColumns: '1fr 1fr' }}>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Amount</span>
            <input
              value={amountText}
              inputMode="decimal"
              onChange={(e) => setAmountText(e.target.value)}
              placeholder="100.00"
              style={{ padding: 10, borderRadius: 8, border: '1px solid #ccc' }}
            />
          </label>
          <div style={{ display: 'grid', gap: 6 }}>
            <span>Currency Pair</span>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <select
                value={sourceCurrency}
                onChange={(e) => setSourceCurrency(normalizeCurrency(e.target.value))}
                style={{ flex: 1, padding: 10, borderRadius: 8, border: '1px solid #ccc' }}
              >
                {currencies.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
              <span>→</span>
              <select
                value={targetCurrency}
                onChange={(e) => setTargetCurrency(normalizeCurrency(e.target.value))}
                style={{ flex: 1, padding: 10, borderRadius: 8, border: '1px solid #ccc' }}
              >
                {currencies.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
        <div style={{ marginTop: 12 }}>
          <button
            type="submit"
            disabled={submitting}
            style={{
              padding: '10px 14px',
              borderRadius: 10,
              border: '1px solid #111',
              background: '#111',
              color: '#fff',
              cursor: 'pointer',
              opacity: submitting ? 0.7 : 1,
            }}
          >
            {submitting ? 'Converting...' : 'Convert'}
          </button>
        </div>
        {error ? (
          <div style={{ marginTop: 12, color: '#b00020' }} role="alert">
            {error}
          </div>
        ) : null}
      </form>

      {result ? (
        <div style={{ border: '1px solid #e5e5e5', padding: 16, borderRadius: 12 }}>
          <h2 style={{ marginTop: 0, fontSize: 16 }}>Converted Result</h2>
          <div style={{ display: 'grid', gap: 8 }}>
            <div>
              <b>{result.originalAmount.toFixed(2)}</b> {result.sourceCurrency} = <b>{result.convertedAmount.toFixed(2)}</b>{' '}
              {result.targetCurrency}
            </div>
            <div>Rate: {result.conversionRate.toFixed(6)}</div>
            <div>Provider date marker: {result.providerDateMarker ?? 'n/a'}</div>
            <div>Provider sequence marker: {result.providerSequenceMarker ?? 'n/a'}</div>
            <div>
              Executed at (UTC): <span style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace' }}>{result.executedAtUtc}</span>
            </div>
            <div style={{ color: '#666' }}>Audit id: {result.id}</div>
          </div>
        </div>
      ) : null}
    </div>
  )
}

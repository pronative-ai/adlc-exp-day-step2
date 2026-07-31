import React, { useState } from 'react'
import { createConversion, getConversion, type ConversionRequest, type ConversionResult } from './api'

function formatOptional(v?: string | null): string {
  if (v === undefined || v === null || v === '') return '—'
  return v
}

export default function App() {
  const [amount, setAmount] = useState<string>('100.00')
  const [fromCurrency, setFromCurrency] = useState<string>('USD')
  const [toCurrency, setToCurrency] = useState<string>('EUR')

  const [auditId, setAuditId] = useState<string>('')
  const [lastResult, setLastResult] = useState<ConversionResult | null>(null)
  const [error, setError] = useState<string>('')
  const [loading, setLoading] = useState<boolean>(false)

  async function onConvert(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const req: ConversionRequest = {
        amount: Number(amount),
        fromCurrency: fromCurrency.trim().toUpperCase(),
        toCurrency: toCurrency.trim().toUpperCase(),
      }
      const result = await createConversion(req)
      setLastResult(result)
      setAuditId(result.auditId)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setLoading(false)
    }
  }

  async function onLookup(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const result = await getConversion(auditId.trim())
      setLastResult(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ maxWidth: 860, margin: '24px auto', padding: 16, fontFamily: 'system-ui, -apple-system, Segoe UI, Roboto, sans-serif' }}>
      <h1 style={{ fontSize: 20, marginBottom: 4 }}>Real-Time Currency Conversion & Audit Trail</h1>
      <p style={{ marginTop: 0, color: '#444' }}>Submit once, then use the audit id to retrieve the immutable record on demand.</p>

      <form onSubmit={onConvert} style={{ border: '1px solid #ddd', borderRadius: 8, padding: 16, marginBottom: 16 }}>
        <h2 style={{ fontSize: 16, marginTop: 0 }}>New Conversion</h2>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
          <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span>Amount</span>
            <input
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              inputMode="decimal"
              style={{ padding: 10, borderRadius: 6, border: '1px solid #ccc' }}
            />
          </label>
          <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span>From</span>
            <input
              value={fromCurrency}
              onChange={(e) => setFromCurrency(e.target.value)}
              style={{ padding: 10, borderRadius: 6, border: '1px solid #ccc' }}
            />
          </label>
          <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span>To</span>
            <input
              value={toCurrency}
              onChange={(e) => setToCurrency(e.target.value)}
              style={{ padding: 10, borderRadius: 6, border: '1px solid #ccc' }}
            />
          </label>
        </div>

        <div style={{ marginTop: 12, display: 'flex', gap: 12, alignItems: 'center' }}>
          <button disabled={loading} type="submit" style={{ padding: '10px 14px', borderRadius: 8, border: 0, background: '#111', color: 'white', cursor: 'pointer' }}>
            {loading ? 'Processing…' : 'Convert'}
          </button>
          {lastResult?.auditId ? (
            <div>
              <div style={{ fontSize: 12, color: '#666' }}>Audit ID</div>
              <div style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace', fontSize: 13 }}>{lastResult.auditId}</div>
            </div>
          ) : null}
        </div>
      </form>

      <form onSubmit={onLookup} style={{ border: '1px solid #ddd', borderRadius: 8, padding: 16, marginBottom: 16 }}>
        <h2 style={{ fontSize: 16, marginTop: 0 }}>Audit Lookup</h2>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 12, alignItems: 'end' }}>
          <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span>Audit ID</span>
            <input
              value={auditId}
              onChange={(e) => setAuditId(e.target.value)}
              placeholder="Paste an audit id"
              style={{ padding: 10, borderRadius: 6, border: '1px solid #ccc' }}
            />
          </label>
          <button disabled={loading || !auditId.trim()} type="submit" style={{ padding: '10px 14px', borderRadius: 8, border: 0, background: '#1f6feb', color: 'white', cursor: 'pointer' }}>
            {loading ? 'Retrieving…' : 'Get Record'}
          </button>
        </div>
      </form>

      {error ? (
        <div style={{ background: '#fff5f5', border: '1px solid #ffd0d0', borderRadius: 8, padding: 12, color: '#b00020', marginBottom: 16 }}>
          <strong>Request failed:</strong> {error}
        </div>
      ) : null}

      {lastResult ? (
        <div style={{ border: '1px solid #ddd', borderRadius: 8, padding: 16 }}>
          <h2 style={{ fontSize: 16, marginTop: 0 }}>Conversion Record</h2>
          <dl style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, margin: 0 }}>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Amount</dt>
              <dd style={{ margin: 0 }}>{lastResult.amount.toString()} {lastResult.fromCurrency}</dd>
            </div>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Converted</dt>
              <dd style={{ margin: 0 }}>{lastResult.convertedAmount.toString()} {lastResult.toCurrency}</dd>
            </div>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Exchange Rate</dt>
              <dd style={{ margin: 0 }}>{lastResult.exchangeRate.toString()}</dd>
            </div>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Backend Execution Timestamp (UTC)</dt>
              <dd style={{ margin: 0, fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace' }}>{lastResult.executionTimestampUtc}</dd>
            </div>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Provider Date Marker</dt>
              <dd style={{ margin: 0, fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace' }}>{formatOptional(lastResult.providerDateMarker)}</dd>
            </div>
            <div>
              <dt style={{ fontSize: 12, color: '#666' }}>Provider Sequence Marker</dt>
              <dd style={{ margin: 0, fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace' }}>{formatOptional(lastResult.providerSequenceMarker)}</dd>
            </div>
          </dl>
        </div>
      ) : null}
    </div>
  )
}

import { useMemo, useState } from 'react'
import { createConversion, getConversion, type ProblemDetails } from './api'

type ConversionResult = {
  id: string
  sourceCurrency: string
  targetCurrency: string
  originalAmount: string | number
  convertedAmount: string | number
  appliedRate: string | number
  providerDate?: string | null
  providerBaseCurrency?: string | null
  providerSequence?: string | null
  backendExecutionTimestampUtc: string
}

function formatProblem(p: ProblemDetails) {
  const title = p.title ?? 'Request failed'
  const status = p.status ? ` (${p.status})` : ''
  return `${title}${status}`
}

export default function App() {
  const [sourceCurrency, setSourceCurrency] = useState('USD')
  const [targetCurrency, setTargetCurrency] = useState('EUR')
  const [amount, setAmount] = useState('100.00')

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<ConversionResult | null>(null)

  const [lookupId, setLookupId] = useState('')
  const [isLookingUp, setIsLookingUp] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [lookupResult, setLookupResult] = useState<ConversionResult | null>(null)

  const normalizedSource = useMemo(() => sourceCurrency.trim().toUpperCase(), [sourceCurrency])
  const normalizedTarget = useMemo(() => targetCurrency.trim().toUpperCase(), [targetCurrency])

  async function onConvert(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setResult(null)

    const parsedAmount = Number(amount)
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError('Amount must be a number greater than 0')
      return
    }

    setIsSubmitting(true)
    try {
      const res = await createConversion({
        sourceCurrency: normalizedSource,
        targetCurrency: normalizedTarget,
        amount: parsedAmount
      })
      setResult(res as ConversionResult)
      setLookupId((res as ConversionResult).id)
    } catch (err: any) {
      setError(formatProblem(err as ProblemDetails))
    } finally {
      setIsSubmitting(false)
    }
  }

  async function onLookup(e: React.FormEvent) {
    e.preventDefault()
    setLookupError(null)
    setLookupResult(null)

    if (!lookupId.trim()) {
      setLookupError('Audit id is required')
      return
    }

    setIsLookingUp(true)
    try {
      const res = await getConversion(lookupId.trim())
      setLookupResult(res as ConversionResult)
    } catch (err: any) {
      setLookupError(formatProblem(err as ProblemDetails))
    } finally {
      setIsLookingUp(false)
    }
  }

  return (
    <div style={{ maxWidth: 860, margin: '0 auto', padding: 16, fontFamily: 'system-ui, sans-serif' }}>
      <h1 style={{ marginBottom: 8 }}>Real-Time Currency Conversion & Audit Trail</h1>
      <p style={{ marginTop: 0, color: '#555' }}>
        Submit a conversion to get an immediate result and an auditable record you can fetch later.
      </p>

      <div
        style={{
          border: '1px solid #e5e7eb',
          borderRadius: 12,
          padding: 16,
          marginTop: 16
        }}
      >
        <h2 style={{ fontSize: 16, marginTop: 0 }}>Live Conversion</h2>
        <form onSubmit={onConvert}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <span>Source Currency (3-letter)</span>
              <input
                value={sourceCurrency}
                onChange={(e) => setSourceCurrency(e.target.value)}
                aria-label="source currency"
                style={{ padding: 10, borderRadius: 10, border: '1px solid #d1d5db' }}
                inputMode="text"
              />
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <span>Target Currency (3-letter)</span>
              <input
                value={targetCurrency}
                onChange={(e) => setTargetCurrency(e.target.value)}
                aria-label="target currency"
                style={{ padding: 10, borderRadius: 10, border: '1px solid #d1d5db' }}
                inputMode="text"
              />
            </label>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <span>Amount</span>
              <input
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                aria-label="amount"
                style={{ padding: 10, borderRadius: 10, border: '1px solid #d1d5db' }}
                inputMode="decimal"
              />
            </label>
          </div>

          <div style={{ marginTop: 12, display: 'flex', gap: 12, alignItems: 'center' }}>
            <button
              type="submit"
              disabled={isSubmitting}
              style={{
                padding: '10px 14px',
                borderRadius: 10,
                border: '1px solid #111827',
                background: '#111827',
                color: 'white',
                cursor: 'pointer'
              }}
            >
              {isSubmitting ? 'Converting...' : 'Convert'}
            </button>
            {error ? <span style={{ color: '#b91c1c' }}>{error}</span> : <span />}
          </div>
        </form>

        {result ? (
          <div style={{ marginTop: 16 }}>
            <h3 style={{ fontSize: 14, marginBottom: 8 }}>Conversion Result</h3>
            <div style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 14 }}>
              <div>Audit Id: <code>{result.id}</code></div>
              <div style={{ marginTop: 8 }}>
                {result.originalAmount} {result.sourceCurrency} = {result.convertedAmount} {result.targetCurrency}
              </div>
              <div style={{ marginTop: 8 }}>Applied Rate: {result.appliedRate}</div>
              <div style={{ marginTop: 8 }}>Provider Date: {result.providerDate ?? '-'}</div>
              <div style={{ marginTop: 8 }}>Backend Execution Timestamp (UTC): {result.backendExecutionTimestampUtc}</div>
            </div>
          </div>
        ) : null}
      </div>

      <div
        style={{
          border: '1px solid #e5e7eb',
          borderRadius: 12,
          padding: 16,
          marginTop: 16
        }}
      >
        <h2 style={{ fontSize: 16, marginTop: 0 }}>Audit Lookup</h2>
        <form onSubmit={onLookup}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 12, alignItems: 'end' }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <span>Audit Id</span>
              <input
                value={lookupId}
                onChange={(e) => setLookupId(e.target.value)}
                aria-label="audit id"
                style={{ padding: 10, borderRadius: 10, border: '1px solid #d1d5db' }}
              />
            </label>
            <button
              type="submit"
              disabled={isLookingUp}
              style={{
                padding: '10px 14px',
                borderRadius: 10,
                border: '1px solid #111827',
                background: '#111827',
                color: 'white',
                cursor: 'pointer'
              }}
            >
              {isLookingUp ? 'Fetching...' : 'Fetch'}
            </button>
          </div>
          {lookupError ? <div style={{ marginTop: 8, color: '#b91c1c' }}>{lookupError}</div> : null}
        </form>

        {lookupResult ? (
          <div style={{ marginTop: 16 }}>
            <h3 style={{ fontSize: 14, marginBottom: 8 }}>Stored Audit Record</h3>
            <div style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 14 }}>
              <div>Audit Id: <code>{lookupResult.id}</code></div>
              <div style={{ marginTop: 8 }}>
                {lookupResult.originalAmount} {lookupResult.sourceCurrency} = {lookupResult.convertedAmount} {lookupResult.targetCurrency}
              </div>
              <div style={{ marginTop: 8 }}>Applied Rate: {lookupResult.appliedRate}</div>
              <div style={{ marginTop: 8 }}>Provider Date: {lookupResult.providerDate ?? '-'}</div>
              <div style={{ marginTop: 8 }}>Provider Base Currency: {lookupResult.providerBaseCurrency ?? '-'}</div>
              <div style={{ marginTop: 8 }}>Provider Sequence: {lookupResult.providerSequence ?? '-'}</div>
              <div style={{ marginTop: 8 }}>Backend Execution Timestamp (UTC): {lookupResult.backendExecutionTimestampUtc}</div>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  )
}

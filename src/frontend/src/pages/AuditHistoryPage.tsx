import React, { useEffect, useState } from 'react'
import { getConversions } from '../api/client'
import type { ConversionAuditRecord } from '../api/types'

export default function AuditHistoryPage() {
  const [limit, setLimit] = useState(20)
  const [items, setItems] = useState<ConversionAuditRecord[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let cancelled = false
    async function run() {
      setLoading(true)
      setError(null)
      try {
        const data = await getConversions(limit)
        if (!cancelled) setItems(data)
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load history')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    run()
    return () => {
      cancelled = true
    }
  }, [limit])

  return (
    <div>
      <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 12 }}>
        <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <span>Limit</span>
          <input
            value={limit}
            type="number"
            min={1}
            max={100}
            onChange={(e) => setLimit(Math.min(100, Math.max(1, Number(e.target.value) || 20)))}
            style={{ width: 96, padding: 10, borderRadius: 8, border: '1px solid #ccc' }}
          />
        </label>
        {loading ? <span>Loading...</span> : null}
      </div>

      {error ? (
        <div style={{ color: '#b00020' }} role="alert">
          {error}
        </div>
      ) : null}

      <div style={{ border: '1px solid #e5e5e5', padding: 16, borderRadius: 12 }}>
        <h2 style={{ marginTop: 0, fontSize: 16 }}>Audit Records</h2>
        {items === null ? (
          <div>—</div>
        ) : items.length === 0 ? (
          <div>No records found.</div>
        ) : (
          <div style={{ display: 'grid', gap: 12 }}>
            {items.map((it) => (
              <div key={it.id} style={{ border: '1px solid #eee', borderRadius: 10, padding: 12 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
                  <div>
                    <div>
                      <b>{it.originalAmount.toFixed(2)}</b> {it.sourceCurrency} → <b>{it.convertedAmount.toFixed(2)}</b> {it.targetCurrency}
                    </div>
                    <div>Rate: {it.conversionRate.toFixed(6)}</div>
                  </div>
                  <div style={{ textAlign: 'right', color: '#666' }}>
                    <div style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace' }}>{it.executedAtUtc}</div>
                    <div>id: {it.id}</div>
                  </div>
                </div>
                <div style={{ marginTop: 8, color: '#666', display: 'flex', gap: 16, flexWrap: 'wrap' }}>
                  <div>Provider date marker: {it.providerDateMarker ?? 'n/a'}</div>
                  <div>Provider sequence marker: {it.providerSequenceMarker ?? 'n/a'}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

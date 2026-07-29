import { useEffect, useState } from 'react'
import { fetchConversionById } from '../api/conversions'

export default function ConversionHistory({ initialValue }) {
  const [id, setId] = useState(initialValue)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [record, setRecord] = useState(null)

  useEffect(() => {
    setId(initialValue)
  }, [initialValue])

  async function lookup(e) {
    e.preventDefault()
    setError('')
    setRecord(null)

    if (!id || id.trim().length < 8) {
      setError('Enter an audit ID.')
      return
    }

    setLoading(true)
    try {
      const rec = await fetchConversionById(id.trim())
      setRecord(rec)
    } catch (err) {
      setError(err?.message || 'Lookup failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h2 style={{ marginTop: 0, fontSize: 14 }}>Audit Lookup</h2>
      <form className="form" onSubmit={lookup}>
        <div className="field">
          <div className="label">Conversion Audit ID</div>
          <input
            className="input mono"
            value={id}
            onChange={(e) => setId(e.target.value)}
            placeholder="e.g. 2b6d2b2e-..."
            aria-label="Audit ID"
          />
        </div>
        <div className="actions">
          <button className="button" type="submit" disabled={loading}>
            {loading ? 'Retrieving…' : 'Retrieve'}
          </button>
        </div>
        {error ? <div className="error">{error}</div> : null}
      </form>

      {record ? (
        <div className="result">
          <h2 className="resultTitle">Stored Conversion</h2>
          <div className="resultRow">
            <span className="label">Audit ID</span>
            <span className="value mono">{record.id}</span>
          </div>
          <div className="resultRow">
            <span className="label">{record.sourceCurrency} → {record.targetCurrency}</span>
            <span className="value mono">{record.convertedAmount}</span>
          </div>
          <div className="resultRow">
            <span className="label">Rate</span>
            <span className="value mono">{record.exchangeRate}</span>
          </div>
          <div className="resultRow">
            <span className="label">Quoted At (UTC)</span>
            <span className="value mono">{new Date(record.quotedAtUtc).toISOString()}</span>
          </div>
          {record.providerResponseId ? (
            <div className="resultRow">
              <span className="label">Provider Response ID</span>
              <span className="value mono">{record.providerResponseId}</span>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  )
}

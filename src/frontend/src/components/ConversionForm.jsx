import { useState } from 'react'
import { quoteConversion } from '../api/conversions'

export default function ConversionForm({ onQuoted }) {
  const [amount, setAmount] = useState('')
  const [sourceCurrency, setSourceCurrency] = useState('USD')
  const [targetCurrency, setTargetCurrency] = useState('EUR')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function submit(e) {
    e.preventDefault()
    setError('')

    const amountNumber = Number(amount)
    if (!Number.isFinite(amountNumber) || amountNumber <= 0) {
      setError('Enter a valid amount greater than 0.')
      return
    }

    setLoading(true)
    try {
      const quote = await quoteConversion({
        amount: amountNumber,
        sourceCurrency,
        targetCurrency
      })
      onQuoted(quote)
    } catch (err) {
      setError(err?.message || 'Failed to quote.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <h2 style={{ marginTop: 0, fontSize: 14 }}>Quote Conversion</h2>
      <form className="form" onSubmit={submit}>
        <div className="row">
          <div className="field">
            <div className="label">Amount</div>
            <input
              className="input"
              inputMode="decimal"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="100.00"
              aria-label="Amount"
            />
          </div>
          <div className="field">
            <div className="label">From</div>
            <input
              className="input"
              value={sourceCurrency}
              onChange={(e) => setSourceCurrency(e.target.value)}
              placeholder="USD"
              aria-label="Source currency"
            />
          </div>
          <div className="field">
            <div className="label">To</div>
            <input
              className="input"
              value={targetCurrency}
              onChange={(e) => setTargetCurrency(e.target.value)}
              placeholder="EUR"
              aria-label="Target currency"
            />
          </div>
        </div>

        <div className="actions">
          <button className="button" type="submit" disabled={loading}>
            {loading ? 'Quoting…' : 'Quote'}
          </button>
        </div>

        {error ? <div className="error">{error}</div> : null}
      </form>
    </div>
  )
}

import { useState } from 'react'
import ConversionForm from './components/ConversionForm.jsx'
import ConversionHistory from './components/ConversionHistory.jsx'
import './styles.css'

export default function App() {
  const [lastQuote, setLastQuote] = useState(null)

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1>Real-Time Currency Conversion</h1>
          <p className="subtitle">Immutable audit trail for regulated conversions.</p>
        </div>
      </header>

      <main className="grid">
        <section className="card">
          <ConversionForm
            onQuoted={(quote) => {
              setLastQuote(quote)
            }}
          />
          {lastQuote ? (
            <div className="result">
              <h2 className="resultTitle">Quote Result</h2>
              <div className="resultRow">
                <span className="label">Audit ID</span>
                <span className="value mono">{lastQuote.id}</span>
              </div>
              <div className="resultRow">
                <span className="label">{lastQuote.sourceCurrency} → {lastQuote.targetCurrency}</span>
                <span className="value mono">{lastQuote.convertedAmount}</span>
              </div>
              <div className="resultRow">
                <span className="label">Rate</span>
                <span className="value mono">{lastQuote.exchangeRate}</span>
              </div>
              <div className="resultRow">
                <span className="label">Quoted At (UTC)</span>
                <span className="value mono">{new Date(lastQuote.quotedAtUtc).toISOString()}</span>
              </div>
              {lastQuote.providerResponseId ? (
                <div className="resultRow">
                  <span className="label">Provider Response ID</span>
                  <span className="value mono">{lastQuote.providerResponseId}</span>
                </div>
              ) : null}
            </div>
          ) : null}
        </section>

        <section className="card">
          <ConversionHistory initialValue={lastQuote?.id ?? ''} />
        </section>
      </main>

      <footer className="footer">
        <span>Audit records are immutable and stored in Cosmos DB.</span>
      </footer>
    </div>
  )
}

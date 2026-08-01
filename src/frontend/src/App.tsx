import { useEffect, useMemo, useState } from 'react';
import { convert, getAuditById, getRecentAudits } from './api/audits';
import type { ConvertResponse } from './api/types';

const currencyOptions = [
  'USD',
  'EUR',
  'GBP',
  'JPY',
  'CAD',
  'AUD',
  'CHF',
  'CNY',
  'INR',
];

function formatUtc(ts: string): string {
  // Keep original precision for auditors; just format into readable local time if parseable.
  const d = new Date(ts);
  if (Number.isNaN(d.getTime())) return ts;
  return `${d.toISOString()}`;
}

export default function App() {
  const [amount, setAmount] = useState<number>(100);
  const [fromCurrency, setFromCurrency] = useState<string>('USD');
  const [toCurrency, setToCurrency] = useState<string>('EUR');

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ConvertResponse | null>(null);

  const [auditId, setAuditId] = useState<string>('');
  const [auditLookupBusy, setAuditLookupBusy] = useState(false);
  const [auditLookupError, setAuditLookupError] = useState<string | null>(null);
  const [auditLookupResult, setAuditLookupResult] = useState<ConvertResponse | null>(null);

  const [history, setHistory] = useState<ConvertResponse[]>([]);
  const [historyError, setHistoryError] = useState<string | null>(null);

  const canConvert = useMemo(() => {
    return amount > 0 && fromCurrency.trim().length > 0 && toCurrency.trim().length > 0;
  }, [amount, fromCurrency, toCurrency]);

  async function loadHistory() {
    setHistoryError(null);
    try {
      const items = await getRecentAudits(10);
      setHistory(items);
    } catch (e) {
      setHistoryError(e instanceof Error ? e.message : String(e));
    }
  }

  useEffect(() => {
    loadHistory();
  }, []);

  async function onConvert() {
    setError(null);
    setResult(null);
    setBusy(true);
    try {
      const r = await convert({ amount, fromCurrency, toCurrency });
      setResult(r);
      await loadHistory();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  async function onAuditLookup() {
    setAuditLookupError(null);
    setAuditLookupResult(null);
    setAuditLookupBusy(true);
    try {
      if (!auditId.trim()) throw new Error('Enter an audit id.');
      const r = await getAuditById(auditId.trim());
      setAuditLookupResult(r);
    } catch (e) {
      setAuditLookupError(e instanceof Error ? e.message : String(e));
    } finally {
      setAuditLookupBusy(false);
    }
  }

  return (
    <div className="page">
      <div className="grid">
        <div className="card">
          <h1 className="title">Real-Time Currency Conversion & Audit Trail</h1>
          <div className="row">
            <div>
              <label>Amount</label>
              <input
                inputMode="decimal"
                type="number"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(Number(e.target.value))}
              />
            </div>
            <div>
              <label>From</label>
              <select value={fromCurrency} onChange={(e) => setFromCurrency(e.target.value)}>
                {currencyOptions.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label>To</label>
              <select value={toCurrency} onChange={(e) => setToCurrency(e.target.value)}>
                {currencyOptions.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="actions" style={{ marginTop: 12 }}>
            <button onClick={onConvert} disabled={!canConvert || busy}>
              {busy ? 'Converting…' : 'Convert now'}
            </button>
            <div className="hint">Conversion result is persisted as an immutable audit record with backend execution time.</div>
          </div>

          {error ? <div className="error">{error}</div> : null}

          {result ? (
            <div className="success">
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Rate:</span>{' '}
                <span className="mono">{result.rate}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Converted:</span>{' '}
                <span className="mono">{result.convertedAmount}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Audit ID:</span>{' '}
                <span className="mono">{result.auditId}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Execution UTC:</span>{' '}
                <span className="mono">{formatUtc(result.executionTimestampUtc)}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Provider date:</span>{' '}
                <span className="mono">{result.providerDate ?? '-'}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Provider marker:</span>{' '}
                <span className="mono">{result.providerSequenceMarker ?? '-'}</span>
              </div>
            </div>
          ) : null}
        </div>

        <div className="card">
          <h2 className="title">Auditor Lookup</h2>

          <div style={{ marginTop: 8 }}>
            <label>Audit ID</label>
            <input value={auditId} onChange={(e) => setAuditId(e.target.value)} placeholder="Paste audit id" />
          </div>

          <div className="actions" style={{ marginTop: 12 }}>
            <button onClick={onAuditLookup} disabled={!auditId.trim() || auditLookupBusy}>
              {auditLookupBusy ? 'Looking up…' : 'Lookup audit'}
            </button>
            <button onClick={() => loadHistory()} disabled={busy || auditLookupBusy} style={{ background: '#2a375a', color: '#e9eef9' }}>
              Refresh recent
            </button>
          </div>

          {auditLookupError ? <div className="error">{auditLookupError}</div> : null}

          {auditLookupResult ? (
            <div className="success" style={{ marginTop: 12 }}>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Audit ID:</span>{' '}
                <span className="mono">{auditLookupResult.auditId}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Rate:</span>{' '}
                <span className="mono">{auditLookupResult.rate}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Converted:</span>{' '}
                <span className="mono">{auditLookupResult.convertedAmount}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Execution UTC:</span>{' '}
                <span className="mono">{formatUtc(auditLookupResult.executionTimestampUtc)}</span>
              </div>
              <div>
                <span style={{ color: 'rgba(233,238,249,0.8)' }}>Provider date:</span>{' '}
                <span className="mono">{auditLookupResult.providerDate ?? '-'}</span>
              </div>
            </div>
          ) : null}

          <h3 className="title" style={{ marginTop: 16 }}>Recent conversions</h3>
          {historyError ? <div className="error">{historyError}</div> : null}
          <div className="list" style={{ marginTop: 10 }}>
            {history.map((h) => (
              <div key={h.auditId} className="item">
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10, alignItems: 'baseline' }}>
                  <div className="mono" style={{ wordBreak: 'break-all' }}>{h.auditId}</div>
                  <div className="mono" style={{ opacity: 0.85 }}>{formatUtc(h.executionTimestampUtc)}</div>
                </div>
                <div style={{ marginTop: 8 }}>
                  <span style={{ color: 'rgba(233,238,249,0.75)' }}>Converted:</span>{' '}
                  <span className="mono">{h.convertedAmount}</span>
                </div>
                <div style={{ marginTop: 4 }}>
                  <span style={{ color: 'rgba(233,238,249,0.75)' }}>Rate:</span>{' '}
                  <span className="mono">{h.rate}</span>
                </div>
                <div style={{ marginTop: 10 }}>
                  <button
                    onClick={() => {
                      setAuditId(h.auditId);
                      setAuditLookupResult(h);
                    }}
                    style={{ width: '100%', background: '#2a375a', color: '#e9eef9' }}
                  >
                    Use this audit
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

import { useState } from "react";
import { convertCurrency, fetchAuditRecord } from "./api.js";

const COMMON_CURRENCIES = [
  "USD",
  "EUR",
  "GBP",
  "INR",
  "JPY",
  "CHF",
  "CAD",
  "AUD",
  "SGD",
  "CNY",
  "AED",
];

function formatNumber(value) {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 4,
  }).format(value);
}

function CurrencyCodes() {
  return (
    <datalist id="currency-codes">
      {COMMON_CURRENCIES.map((code) => (
        <option key={code} value={code} />
      ))}
    </datalist>
  );
}

function ResultCard({ result }) {
  return (
    <section className="card" aria-label="Conversion result">
      <h2>Conversion result</h2>
      <dl className="result-grid">
        <dt>Converted amount</dt>
        <dd>
          {formatNumber(result.convertedAmount)} {result.to}
        </dd>
        <dt>Rate</dt>
        <dd>{formatNumber(result.rate)}</dd>
        <dt>Provider</dt>
        <dd>{result.provider}</dd>
        <dt>Provider date</dt>
        <dd>{result.providerDate || "—"}</dd>
        <dt>Server timestamp</dt>
        <dd>{result.serverTimestamp}</dd>
        <dt>Audit id</dt>
        <dd className="mono">{result.auditId}</dd>
      </dl>
      {result.rateIsStale && (
        <p className="stale-banner" role="status">
          Stale rate — the provider was unavailable, so the last known rate was used.
        </p>
      )}
    </section>
  );
}

function AuditRecordCard({ record }) {
  return (
    <section className="card" aria-label="Audit record">
      <h2>Audit record</h2>
      <dl className="result-grid">
        <dt>Audit id</dt>
        <dd className="mono">{record.id}</dd>
        <dt>Tenant</dt>
        <dd>{record.tenantId}</dd>
        <dt>Amount</dt>
        <dd>
          {formatNumber(record.amount)} {record.fromCurrency}
        </dd>
        <dt>Converted to</dt>
        <dd>
          {formatNumber(record.convertedAmount ?? record.amount * record.rate)} {record.toCurrency}
        </dd>
        <dt>Rate</dt>
        <dd>{formatNumber(record.rate)}</dd>
        <dt>Provider</dt>
        <dd>{record.provider}</dd>
        <dt>Provider date</dt>
        <dd>{record.providerDate || "—"}</dd>
        <dt>Server timestamp</dt>
        <dd>{record.serverTimestamp}</dd>
      </dl>
      {record.rateIsStale && (
        <p className="stale-banner" role="status">
          Stale rate — this conversion used a fallback rate.
        </p>
      )}
    </section>
  );
}

function ErrorBanner({ message }) {
  if (!message) return null;
  return (
    <p className="error-banner" role="alert">
      {message}
    </p>
  );
}

function ConversionForm({ onResult, onError }) {
  const [amount, setAmount] = useState("");
  const [from, setFrom] = useState("USD");
  const [to, setTo] = useState("EUR");
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setBusy(true);
    onError("");
    try {
      const result = await convertCurrency({
        amount: Number(amount),
        from: from.trim().toUpperCase(),
        to: to.trim().toUpperCase(),
      });
      onResult(result);
    } catch (error) {
      onResult(null);
      onError(error.message || "Conversion failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={handleSubmit}>
      <h2>Convert currency</h2>
      <div className="form-row">
        <label>
          Amount
          <input
            type="number"
            min="0.01"
            step="0.01"
            required
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
          />
        </label>
        <label>
          From
          <input
            type="text"
            maxLength="3"
            required
            list="currency-codes"
            value={from}
            onChange={(event) => setFrom(event.target.value)}
          />
        </label>
        <label>
          To
          <input
            type="text"
            maxLength="3"
            required
            list="currency-codes"
            value={to}
            onChange={(event) => setTo(event.target.value)}
          />
        </label>
      </div>
      <button type="submit" disabled={busy}>
        {busy ? "Converting…" : "Convert"}
      </button>
      <CurrencyCodes />
    </form>
  );
}

function AuditLookup({ onRecord, onError }) {
  const [auditId, setAuditId] = useState("");
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setBusy(true);
    onError("");
    try {
      const record = await fetchAuditRecord(auditId.trim());
      onRecord(record);
    } catch (error) {
      onRecord(null);
      onError(error.message || "Audit lookup failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="card" onSubmit={handleSubmit}>
      <h2>Look up an audit record</h2>
      <div className="form-row">
        <label>
          Audit id
          <input
            type="text"
            required
            placeholder="00000000-0000-0000-0000-000000000000"
            value={auditId}
            onChange={(event) => setAuditId(event.target.value)}
          />
        </label>
      </div>
      <button type="submit" disabled={busy}>
        {busy ? "Fetching…" : "Fetch record"}
      </button>
    </form>
  );
}

export default function App() {
  const [result, setResult] = useState(null);
  const [auditRecord, setAuditRecord] = useState(null);
  const [error, setError] = useState("");

  return (
    <div className="app">
      <header>
        <h1>Real-Time Currency Conversion &amp; Audit Trail</h1>
        <p>
          Convert currency instantly and keep an audit-trail record for compliance.
        </p>
      </header>
      <ErrorBanner message={error} />
      <ConversionForm
        onResult={(value) => {
          setResult(value);
          setAuditRecord(null);
        }}
        onError={setError}
      />
      {result && <ResultCard result={result} />}
      <AuditLookup
        onRecord={(value) => {
          setAuditRecord(value);
          setResult(null);
        }}
        onError={setError}
      />
      {auditRecord && <AuditRecordCard record={auditRecord} />}
    </div>
  );
}

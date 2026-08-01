import { FormEvent, useState } from 'react';
import { getConversionAudit } from '../lib/api';
import type { CurrencyConversionAuditResponse } from '../lib/types';

export function ConversionAuditLookup() {
  const [auditId, setAuditId] = useState('');
  const [result, setResult] = useState<CurrencyConversionAuditResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  async function handleLookup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await getConversionAudit(auditId.trim());
      setResult(response);
    } catch (error) {
      setResult(null);
      setErrorMessage(error instanceof Error ? error.message : 'Unable to retrieve audit record.');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <section className="card panel">
      <div className="panel-heading">
        <h2>Audit lookup</h2>
        <p>Retrieve the exact stored conversion record without recalculating against a newer rate.</p>
      </div>

      <form className="form-grid" onSubmit={handleLookup}>
        <label>
          <span>Audit id</span>
          <input
            name="auditId"
            placeholder="Paste a conversion audit id"
            value={auditId}
            onChange={(event) => setAuditId(event.target.value)}
            required
          />
        </label>

        <button type="submit" disabled={isLoading}>
          {isLoading ? 'Loading…' : 'Lookup audit record'}
        </button>
      </form>

      {errorMessage ? <p className="status error">{errorMessage}</p> : null}

      {result ? (
        <article className="result-card" aria-live="polite">
          <div className="result-header">
            <h3>Stored audit record</h3>
            <span className="pill">{result.auditId}</span>
          </div>

          <dl className="result-grid">
            <div>
              <dt>Pair</dt>
              <dd>
                {result.sourceCurrency} → {result.targetCurrency}
              </dd>
            </div>
            <div>
              <dt>Original amount</dt>
              <dd>{result.originalAmount.toFixed(2)}</dd>
            </div>
            <div>
              <dt>Converted amount</dt>
              <dd>{result.convertedAmount.toFixed(2)}</dd>
            </div>
            <div>
              <dt>Rate used</dt>
              <dd>{result.rate}</dd>
            </div>
            <div>
              <dt>Executed at (UTC)</dt>
              <dd>{result.executedAtUtc}</dd>
            </div>
            <div>
              <dt>Provider date</dt>
              <dd>{result.providerDate ?? 'Not supplied'}</dd>
            </div>
          </dl>
        </article>
      ) : null}
    </section>
  );
}

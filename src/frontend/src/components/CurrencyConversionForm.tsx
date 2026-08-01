import { FormEvent, useMemo, useState } from 'react';
import { createConversion } from '../lib/api';
import type { CurrencyConversionAuditResponse } from '../lib/types';

const initialFormState = {
  amount: '100.00',
  sourceCurrency: 'USD',
  targetCurrency: 'EUR',
};

export function CurrencyConversionForm() {
  const [formState, setFormState] = useState(initialFormState);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [result, setResult] = useState<CurrencyConversionAuditResponse | null>(null);

  const amountPreview = useMemo(() => Number.parseFloat(formState.amount || '0'), [formState.amount]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const response = await createConversion({
        amount: Number.parseFloat(formState.amount),
        sourceCurrency: formState.sourceCurrency,
        targetCurrency: formState.targetCurrency,
      });

      setResult(response);
    } catch (error) {
      setResult(null);
      setErrorMessage(error instanceof Error ? error.message : 'Unable to complete conversion.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="card panel">
      <div className="panel-heading">
        <h2>Convert currency</h2>
        <p>Get a live rate, converted amount, execution timestamp, and audit id in one step.</p>
      </div>

      <form className="form-grid" onSubmit={handleSubmit}>
        <label>
          <span>Amount</span>
          <input
            name="amount"
            inputMode="decimal"
            value={formState.amount}
            onChange={(event) => setFormState((current) => ({ ...current, amount: event.target.value }))}
            required
          />
        </label>

        <label>
          <span>Source currency</span>
          <input
            name="sourceCurrency"
            maxLength={3}
            value={formState.sourceCurrency}
            onChange={(event) =>
              setFormState((current) => ({ ...current, sourceCurrency: event.target.value.toUpperCase() }))
            }
            required
          />
        </label>

        <label>
          <span>Target currency</span>
          <input
            name="targetCurrency"
            maxLength={3}
            value={formState.targetCurrency}
            onChange={(event) =>
              setFormState((current) => ({ ...current, targetCurrency: event.target.value.toUpperCase() }))
            }
            required
          />
        </label>

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Converting…' : 'Convert now'}
        </button>
      </form>

      <div className="inline-hint">
        Preview: {Number.isFinite(amountPreview) ? amountPreview.toFixed(2) : '0.00'} {formState.sourceCurrency || '---'}
      </div>

      {errorMessage ? <p className="status error">{errorMessage}</p> : null}

      {result ? (
        <article className="result-card" aria-live="polite">
          <div className="result-header">
            <h3>Conversion result</h3>
            <span className="pill">Audit id: {result.auditId}</span>
          </div>

          <dl className="result-grid">
            <div>
              <dt>Original amount</dt>
              <dd>
                {result.originalAmount.toFixed(2)} {result.sourceCurrency}
              </dd>
            </div>
            <div>
              <dt>Converted amount</dt>
              <dd>
                {result.convertedAmount.toFixed(2)} {result.targetCurrency}
              </dd>
            </div>
            <div>
              <dt>Rate</dt>
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
            <div>
              <dt>Provider sequence</dt>
              <dd>{result.providerSequenceMarker ?? 'Not supplied'}</dd>
            </div>
          </dl>

          <p className="provider-note">Provider base URL: {result.providerBaseUrl}</p>
        </article>
      ) : null}
    </section>
  );
}

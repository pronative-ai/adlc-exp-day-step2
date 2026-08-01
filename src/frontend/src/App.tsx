import { ConversionAuditLookup } from './components/ConversionAuditLookup';
import { CurrencyConversionForm } from './components/CurrencyConversionForm';

export default function App() {
  return (
    <main className="app-shell">
      <section className="hero card">
        <p className="eyebrow">Treasury Operations</p>
        <h1>Real-Time Currency Conversion &amp; Audit Trail</h1>
        <p className="hero-copy">
          Convert settlement amounts instantly, capture the exact rate and backend execution timestamp,
          and retrieve any prior conversion on demand with its audit identifier.
        </p>
      </section>

      <div className="content-grid">
        <CurrencyConversionForm />
        <ConversionAuditLookup />
      </div>
    </main>
  );
}

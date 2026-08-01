import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import App from "./App.jsx";

function jsonResponse(status, body) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

describe("App", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => jsonResponse(200, {})),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders the conversion form and audit lookup", () => {
    render(<App />);
    expect(screen.getByRole("heading", { name: "Convert currency" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Look up an audit record" })).toBeInTheDocument();
  });

  it("submits a conversion and shows the result", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        jsonResponse(200, {
          amount: 1000,
          from: "USD",
          to: "EUR",
          convertedAmount: 918.3,
          rate: 0.9183,
          provider: "Frankfurter",
          providerDate: "2026-08-01",
          serverTimestamp: "2026-08-01T09:15:32.1234567Z",
          rateIsStale: false,
          auditId: "11111111-2222-3333-4444-555555555555",
        }),
      ),
    );

    render(<App />);
    fireEvent.change(screen.getByLabelText("Amount"), { target: { value: "1000" } });
    fireEvent.change(screen.getByLabelText("From"), { target: { value: "USD" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "EUR" } });
    fireEvent.click(screen.getByRole("button", { name: "Convert" }));

    await waitFor(() =>
      expect(screen.getByText("918.3 EUR")).toBeInTheDocument(),
    );
    expect(screen.getByText("0.9183")).toBeInTheDocument();
    expect(screen.getByText("11111111-2222-3333-4444-555555555555")).toBeInTheDocument();
    expect(screen.queryByText(/Stale rate/)).not.toBeInTheDocument();
  });

  it("shows the stale-rate banner when a fallback rate is returned", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        jsonResponse(200, {
          amount: 100,
          from: "USD",
          to: "EUR",
          convertedAmount: 91.83,
          rate: 0.9183,
          provider: "Frankfurter",
          providerDate: "2026-08-01",
          serverTimestamp: "2026-08-01T09:15:32.1234567Z",
          rateIsStale: true,
          auditId: "11111111-2222-3333-4444-555555555555",
        }),
      ),
    );

    render(<App />);
    fireEvent.change(screen.getByLabelText("Amount"), { target: { value: "100" } });
    fireEvent.change(screen.getByLabelText("From"), { target: { value: "USD" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "EUR" } });
    fireEvent.click(screen.getByRole("button", { name: "Convert" }));

    await waitFor(() =>
      expect(screen.getByText(/Stale rate/)).toBeInTheDocument(),
    );
  });

  it("shows the problem-details message when the provider is unavailable", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        jsonResponse(503, {
          title: "RateProviderUnavailable",
          detail: "The rate provider is currently unavailable.",
        }),
      ),
    );

    render(<App />);
    fireEvent.change(screen.getByLabelText("Amount"), { target: { value: "100" } });
    fireEvent.change(screen.getByLabelText("From"), { target: { value: "USD" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "EUR" } });
    fireEvent.click(screen.getByRole("button", { name: "Convert" }));

    await waitFor(() =>
      expect(screen.getByText("The rate provider is currently unavailable.")).toBeInTheDocument(),
    );
    expect(screen.queryByRole("heading", { name: "Conversion result" })).not.toBeInTheDocument();
  });

  it("fetches and shows an audit record by id", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        jsonResponse(200, {
          id: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          tenantId: "default",
          amount: 500,
          fromCurrency: "USD",
          toCurrency: "EUR",
          convertedAmount: 459.15,
          rate: 0.9183,
          provider: "Frankfurter",
          providerDate: "2026-08-01",
          serverTimestamp: "2026-08-01T09:15:32.1234567Z",
          rateIsStale: false,
        }),
      ),
    );

    render(<App />);
    fireEvent.change(screen.getByLabelText("Audit id"), {
      target: { value: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Fetch record" }));

    await waitFor(() =>
      expect(screen.getByRole("heading", { name: "Audit record" })).toBeInTheDocument(),
    );
    expect(screen.getByText("459.15 EUR")).toBeInTheDocument();
  });

  it("shows a 404 message for an unknown audit id", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        jsonResponse(404, {
          title: "AuditRecordNotFound",
          detail: "No audit record found for id 'unknown'.",
        }),
      ),
    );

    render(<App />);
    fireEvent.change(screen.getByLabelText("Audit id"), {
      target: { value: "unknown" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Fetch record" }));

    await waitFor(() =>
      expect(screen.getByText("No audit record found for id 'unknown'.")).toBeInTheDocument(),
    );
  });
});

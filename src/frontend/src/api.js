const PLACEHOLDER = "__VITE_API_URL__";

export function resolveApiBaseUrl() {
  const configured = typeof window !== "undefined" ? window.__VITE_API_URL__ : undefined;
  if (configured && configured !== PLACEHOLDER) {
    return configured.replace(/\/+$/, "");
  }
  return "";
}

export const apiBaseUrl = resolveApiBaseUrl();

export async function convertCurrency({ amount, from, to }) {
  const response = await fetch(`${apiBaseUrl}/api/currency/convert`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ amount, from, to }),
  });
  return parseJsonResponse(response);
}

export async function fetchAuditRecord(auditId) {
  const response = await fetch(
    `${apiBaseUrl}/api/currency/audit/${encodeURIComponent(auditId)}`,
  );
  return parseJsonResponse(response);
}

async function parseJsonResponse(response) {
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const error = new Error(data?.detail || data?.title || `Request failed with status ${response.status}`);
    error.status = response.status;
    error.title = data?.title;
    error.detail = data?.detail;
    throw error;
  }
  return data;
}

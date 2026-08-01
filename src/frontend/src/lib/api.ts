import type {
  CreateCurrencyConversionRequest,
  CurrencyConversionAuditResponse,
  ProblemDetails,
} from './types';

function getRuntimeApiUrl() {
  const configuredValue = window.__APP_CONFIG__?.VITE_API_URL?.trim();
  if (!configuredValue || configuredValue === '__VITE_API_URL__') {
    return '';
  }

  return configuredValue.replace(/\/$/, '');
}

function buildApiUrl(path: string) {
  const baseUrl = getRuntimeApiUrl();
  return baseUrl ? `${baseUrl}${path}` : path;
}

async function parseError(response: Response) {
  try {
    const payload = (await response.json()) as ProblemDetails;
    return payload.detail || payload.title || 'Request failed.';
  } catch {
    return 'Request failed.';
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(buildApiUrl(path), {
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    ...init,
  });

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return (await response.json()) as T;
}

export function createConversion(requestBody: CreateCurrencyConversionRequest) {
  return request<CurrencyConversionAuditResponse>('/api/conversions', {
    method: 'POST',
    body: JSON.stringify(requestBody),
  });
}

export function getConversionAudit(auditId: string) {
  return request<CurrencyConversionAuditResponse>(`/api/conversions/${encodeURIComponent(auditId)}`);
}

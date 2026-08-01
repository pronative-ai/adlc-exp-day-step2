export interface CreateCurrencyConversionRequest {
  amount: number;
  sourceCurrency: string;
  targetCurrency: string;
}

export interface CurrencyConversionAuditResponse {
  auditId: string;
  sourceCurrency: string;
  targetCurrency: string;
  originalAmount: number;
  rate: number;
  convertedAmount: number;
  providerDate: string | null;
  providerSequenceMarker: string | null;
  providerBaseUrl: string;
  executedAtUtc: string;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

export type ConvertRequest = {
  amount: number;
  fromCurrency: string;
  toCurrency: string;
};

export type ConvertResponse = {
  auditId: string;
  rate: number;
  convertedAmount: number;
  executionTimestampUtc: string;
  providerDate?: string | null;
  providerSequenceMarker?: string | null;
};

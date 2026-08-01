export type CurrencyCode = string

export type CreateConversionRequest = {
  amount: number
  sourceCurrency: CurrencyCode
  targetCurrency: CurrencyCode
}

export type CreateConversionResponse = {
  id: string
  sourceCurrency: CurrencyCode
  targetCurrency: CurrencyCode
  originalAmount: number
  conversionRate: number
  convertedAmount: number
  providerDateMarker?: string | null
  providerSequenceMarker?: string | null
  executedAtUtc: string
}

export type ConversionAuditRecord = CreateConversionResponse & {
  // stable auditing document id
}

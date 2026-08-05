import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  CreateQuoteRequest,
  Quote,
  QuoteComparison,
  QuoteComparisonParams,
  QuoteListParams,
  QuoteSummary,
} from "@/features/quotes/types/quote.types";

/**
 * `POST /quotes` — teklif oluşturur (fiyatlama motoru çağrılır, Priced döner).
 * customerId verilirse acente destekli akış: `POST /customers/{customerId}/quotes` — personel müşteri ADINA
 * oluşturur (teklif sahibi yine müşteridir). Aynı istek gövdesi kullanılır (kod tekrarı yok).
 */
export async function createQuote(request: CreateQuoteRequest, customerId?: string): Promise<Quote> {
  const url = customerId ? `/customers/${customerId}/quotes` : "/quotes";
  const response = await api.post<Quote>(url, request);
  return response.data;
}

/**
 * `GET /quotes/compare` — aynı risk objesi için teminat seviyeli alternatifler (önizleme).
 * customerId verilirse acente destekli önizleme: `GET /customers/{customerId}/quotes/compare`.
 */
export async function getQuoteComparison(
  params: QuoteComparisonParams,
  customerId?: string,
): Promise<QuoteComparison> {
  const url = customerId ? `/customers/${customerId}/quotes/compare` : "/quotes/compare";
  const response = await api.get<QuoteComparison>(url, { params });
  return response.data;
}

/** `GET /quotes` — müşterinin teklif listesi (sayfalı, filtreli). */
export async function getQuotes(params: QuoteListParams): Promise<PagedResult<QuoteSummary>> {
  const response = await api.get<PagedResult<QuoteSummary>>("/quotes", { params });
  return response.data;
}

/** `GET /quotes/{id}` — teklif detayı (prim dökümü ile). */
export async function getQuoteById(id: string): Promise<Quote> {
  const response = await api.get<Quote>(`/quotes/${id}`);
  return response.data;
}

/** `POST /quotes/{id}/approve` — Priced → Approved. */
export async function approveQuote(id: string): Promise<QuoteSummary> {
  const response = await api.post<QuoteSummary>(`/quotes/${id}/approve`);
  return response.data;
}

/** `POST /quotes/{id}/reject` — teklifi reddeder. */
export async function rejectQuote(id: string): Promise<QuoteSummary> {
  const response = await api.post<QuoteSummary>(`/quotes/${id}/reject`);
  return response.data;
}

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

/** `POST /quotes` — teklif oluşturur (fiyatlama motoru çağrılır, Priced döner). */
export async function createQuote(request: CreateQuoteRequest): Promise<Quote> {
  const response = await api.post<Quote>("/quotes", request);
  return response.data;
}

/** `GET /quotes/compare` — aynı risk objesi için teminat seviyeli alternatifler (önizleme). */
export async function getQuoteComparison(params: QuoteComparisonParams): Promise<QuoteComparison> {
  const response = await api.get<QuoteComparison>("/quotes/compare", { params });
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

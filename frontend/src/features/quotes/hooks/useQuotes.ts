import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  approveQuote,
  createQuote,
  getQuoteById,
  getQuoteComparison,
  getQuotes,
  rejectQuote,
} from "@/features/quotes/services/quotesApi";
import type {
  QuoteComparisonParams,
  QuoteListParams,
} from "@/features/quotes/types/quote.types";

export const quotesQueryKeys = {
  all: ["quotes"] as const,
  list: (params: QuoteListParams) => ["quotes", "list", params] as const,
  detail: (id: string) => ["quotes", "detail", id] as const,
  // customerId önbelleği ayırır: self-servis ile acente destekli önizleme çakışmaz.
  comparison: (params: QuoteComparisonParams, customerId?: string) =>
    ["quotes", "comparison", customerId ?? "self", params] as const,
};

/** Teklif listesi (sayfalı, filtreli). */
export function useQuoteList(params: QuoteListParams) {
  return useQuery({
    queryKey: quotesQueryKeys.list(params),
    queryFn: () => getQuotes(params),
  });
}

/** Teklif detayı. */
export function useQuote(id: string) {
  return useQuery({
    queryKey: quotesQueryKeys.detail(id),
    queryFn: () => getQuoteById(id),
  });
}

/**
 * Paket karşılaştırması (anlık prim + risk skoru). `enabled` ile sihirbazın risk
 * objesi adımı tamamlanmadan çağrılması engellenir. customerId verilirse acente destekli
 * önizleme (personel, müşteri adına) — aynı sorgu/önbellek yolu kullanılır (kod tekrarı yok).
 */
export function useQuoteComparison(
  params: QuoteComparisonParams,
  enabled: boolean,
  customerId?: string,
) {
  return useQuery({
    queryKey: quotesQueryKeys.comparison(params, customerId),
    queryFn: () => getQuoteComparison(params, customerId),
    enabled,
  });
}

/** customerId verilirse acente destekli teklif oluşturma (personel, müşteri adına). */
export function useCreateQuote(customerId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: Parameters<typeof createQuote>[0]) => createQuote(request, customerId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: quotesQueryKeys.all }),
  });
}

export function useApproveQuote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => approveQuote(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: quotesQueryKeys.all }),
  });
}

export function useRejectQuote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => rejectQuote(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: quotesQueryKeys.all }),
  });
}

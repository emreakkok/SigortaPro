import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptRenewal,
  getMyRenewals,
} from "@/features/renewals/services/renewalsApi";
import { quotesQueryKeys } from "@/features/quotes/hooks/useQuotes";
import type { RenewalListParams } from "@/features/renewals/types/renewal.types";

export const renewalsQueryKeys = {
  all: ["renewals"] as const,
  list: (params: RenewalListParams) => ["renewals", "list", params] as const,
};

/** Yenileme teklifleri (sayfalı). */
export function useRenewalList(params: RenewalListParams) {
  return useQuery({
    queryKey: renewalsQueryKeys.list(params),
    queryFn: () => getMyRenewals(params),
  });
}

/**
 * Yenilemeyi onaylar. Onay, yeni dönem teklifini Approved'a çeker; ilgili yenileme ve teklif
 * cache'leri geçersizleştirilir (ardından müşteri ödeme akışına — Task 18 — yönlenir).
 */
export function useAcceptRenewal() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => acceptRenewal(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: renewalsQueryKeys.all });
      queryClient.invalidateQueries({ queryKey: quotesQueryKeys.all });
    },
  });
}

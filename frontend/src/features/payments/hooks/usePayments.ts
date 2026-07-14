import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getInstallmentOptions,
  purchaseQuote,
} from "@/features/payments/services/paymentsApi";
import { policiesQueryKeys } from "@/features/policies/hooks/usePolicies";
import { quotesQueryKeys } from "@/features/quotes/hooks/useQuotes";

export const paymentsQueryKeys = {
  installmentOptions: (quoteId: string) => ["payments", "installment-options", quoteId] as const,
};

/** Onaylanmış teklifin taksit seçenekleri (ödeme sayfası önizlemesi). */
export function useInstallmentOptions(quoteId: string, enabled: boolean) {
  return useQuery({
    queryKey: paymentsQueryKeys.installmentOptions(quoteId),
    queryFn: () => getInstallmentOptions(quoteId),
    enabled,
  });
}

/**
 * Teklif satın alma (mock POS). Başarıda teklif Purchased olur ve yeni poliçe oluşur; ilgili teklif ve
 * poliçe cache'leri geçersizleştirilir. Başarısız ödeme (402) mutation.error olarak yüzeye çıkar.
 */
export function usePurchaseQuote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: purchaseQuote,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: quotesQueryKeys.all });
      queryClient.invalidateQueries({ queryKey: policiesQueryKeys.all });
    },
  });
}

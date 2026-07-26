import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createPricingVersion,
  getPricingVersions,
} from "@/features/pricing/services/pricingApi";

export const pricingQueryKeys = {
  all: ["pricing"] as const,
  versions: ["pricing", "versions"] as const,
};

/** Yürürlükteki tarife + fiyatlandırma geçmişi (tek sorgu — ADR-048). */
export function usePricingVersions() {
  return useQuery({
    queryKey: pricingQueryKeys.versions,
    queryFn: getPricingVersions,
  });
}

/**
 * Yeni tarife versiyonu yayınlar. Başarıda liste tazelenir; mevcut teklif/poliçe cache'lerine
 * dokunulmaz — çünkü onların fiyatları değişmez (sabitlenmiş versiyonlarıyla hesaplanır).
 */
export function useCreatePricingVersion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createPricingVersion,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pricingQueryKeys.all }),
  });
}

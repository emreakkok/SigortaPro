import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  activatePricingVersion,
  createPricingDraft,
  discardPricingDraft,
  getPricingVersions,
  updatePricingDraft,
} from "@/features/pricing/services/pricingApi";
import type {
  CreatePricingDraftRequest,
  UpdatePricingDraftRequest,
} from "@/features/pricing/types/pricing.types";

export const pricingQueryKeys = {
  all: ["pricing"] as const,
  versions: ["pricing", "versions"] as const,
};

/** Yürürlükteki tarife + taslak + geçmiş (tek sorgu — ADR-048). */
export function usePricingVersions() {
  return useQuery({
    queryKey: pricingQueryKeys.versions,
    queryFn: getPricingVersions,
  });
}

/**
 * Fiyatlandırma mutasyonları. Başarıda YALNIZCA tarife listesi tazelenir; mevcut teklif/poliçe cache'lerine
 * dokunulmaz — çünkü onların fiyatları değişmez (sabitlenmiş versiyonlarıyla hesaplanır).
 */
export function useCreatePricingDraft() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePricingDraftRequest) => createPricingDraft(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pricingQueryKeys.all }),
  });
}

export function useUpdatePricingDraft() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdatePricingDraftRequest }) =>
      updatePricingDraft(id, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pricingQueryKeys.all }),
  });
}

export function useActivatePricingVersion() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => activatePricingVersion(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pricingQueryKeys.all }),
  });
}

export function useDiscardPricingDraft() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => discardPricingDraft(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: pricingQueryKeys.all }),
  });
}

import { useMutation, useQuery } from "@tanstack/react-query";
import {
  downloadPolicyDocument,
  getMyPolicies,
  getPolicyById,
} from "@/features/policies/services/policiesApi";
import type { PolicyListParams } from "@/features/policies/types/policy.types";

export const policiesQueryKeys = {
  all: ["policies"] as const,
  list: (params: PolicyListParams) => ["policies", "list", params] as const,
  detail: (id: string) => ["policies", "detail", id] as const,
};

/** Poliçe listesi (sayfalı, opsiyonel durum filtresi). */
export function usePolicyList(params: PolicyListParams) {
  return useQuery({
    queryKey: policiesQueryKeys.list(params),
    queryFn: () => getMyPolicies(params),
  });
}

/** Poliçe detayı (teminat tablosu). */
export function usePolicy(id: string) {
  return useQuery({
    queryKey: policiesQueryKeys.detail(id),
    queryFn: () => getPolicyById(id),
  });
}

/** Poliçe PDF'ini indirir. İndirme bir yan etki olduğundan mutation olarak modellenir (isPending/isError). */
export function useDownloadPolicyDocument() {
  return useMutation({
    mutationFn: ({ policyId, policyNumber }: { policyId: string; policyNumber: string }) =>
      downloadPolicyDocument(policyId, policyNumber),
  });
}

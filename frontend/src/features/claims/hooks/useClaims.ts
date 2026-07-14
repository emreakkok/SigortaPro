import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  approveClaim,
  createClaim,
  getClaimById,
  getClaims,
  payClaim,
  rejectClaim,
  startClaimReview,
} from "@/features/claims/services/claimsApi";
import type {
  ApproveClaimRequest,
  ClaimListParams,
  RejectClaimRequest,
} from "@/features/claims/types/claim.types";

export const claimsQueryKeys = {
  all: ["claims"] as const,
  list: (params: ClaimListParams) => ["claims", "list", params] as const,
  detail: (id: string) => ["claims", "detail", id] as const,
};

/** Hasar listesi (sayfalı, filtreli). */
export function useClaimList(params: ClaimListParams) {
  return useQuery({
    queryKey: claimsQueryKeys.list(params),
    queryFn: () => getClaims(params),
  });
}

/** Hasar detayı. */
export function useClaim(id: string) {
  return useQuery({
    queryKey: claimsQueryKeys.detail(id),
    queryFn: () => getClaimById(id),
  });
}

/** Hasar bildirimi. Başarıda hasar listesi geçersizleştirilir. */
export function useCreateClaim() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createClaim,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: claimsQueryKeys.all }),
  });
}

/** Hasar durum-geçişi mutation'larının ortak invalidation davranışı (liste + detay tazelenir). */
function useClaimTransition<TVariables>(mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: claimsQueryKeys.all }),
  });
}

/** Hasarı incelemeye alır (Submitted → UnderReview; personel). */
export function useStartClaimReview() {
  return useClaimTransition((id: string) => startClaimReview(id));
}

/** İncelemedeki hasarı onaylar (onay tutarı + not; personel). */
export function useApproveClaim() {
  return useClaimTransition(
    ({ id, request }: { id: string; request: ApproveClaimRequest }) => approveClaim(id, request),
  );
}

/** İncelemedeki hasarı reddeder (gerekçe zorunlu; personel). */
export function useRejectClaim() {
  return useClaimTransition(
    ({ id, request }: { id: string; request: RejectClaimRequest }) => rejectClaim(id, request),
  );
}

/** Onaylanmış hasarın ödemesini gerçekleştirir (Approved → Paid; personel). */
export function usePayClaim() {
  return useClaimTransition((id: string) => payClaim(id));
}

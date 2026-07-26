import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  ApproveClaimRequest,
  Claim,
  ClaimListParams,
  ClaimSummary,
  CreateClaimRequest,
  RejectClaimRequest,
} from "@/features/claims/types/claim.types";

/** `POST /claims` — aktif poliçe için hasar bildirir (Submitted). */
export async function createClaim(request: CreateClaimRequest): Promise<Claim> {
  const response = await api.post<Claim>("/claims", request);
  return response.data;
}

/** `GET /claims` — müşterinin hasar listesi (sayfalı, filtreli). */
export async function getClaims(params: ClaimListParams): Promise<PagedResult<ClaimSummary>> {
  const response = await api.get<PagedResult<ClaimSummary>>("/claims", { params });
  return response.data;
}

/** `GET /claims/{id}` — hasar detayı (durum + değerlendirme notu + belgeler). */
export async function getClaimById(id: string): Promise<Claim> {
  const response = await api.get<Claim>(`/claims/${id}`);
  return response.data;
}

/**
 * `GET /claims/{id}/documents/{documentId}` — belge içeriğini blob olarak indirir (Authorization header
 * axios interceptor ile eklenir; korumalı görsel/PDF `<img>`/`<a>` ile gösterilemeyeceğinden blob çekilir).
 */
export async function getClaimDocumentBlob(claimId: string, documentId: string): Promise<Blob> {
  const response = await api.get(`/claims/${claimId}/documents/${documentId}`, { responseType: "blob" });
  return response.data as Blob;
}

/** `POST /claims/{id}/start-review` — hasarı incelemeye alır (Submitted → UnderReview; personel). */
export async function startClaimReview(id: string): Promise<ClaimSummary> {
  const response = await api.post<ClaimSummary>(`/claims/${id}/start-review`);
  return response.data;
}

/** `POST /claims/{id}/approve` — incelemedeki hasarı onaylar (onay tutarı + not; personel). */
export async function approveClaim(
  id: string,
  request: ApproveClaimRequest,
): Promise<ClaimSummary> {
  const response = await api.post<ClaimSummary>(`/claims/${id}/approve`, request);
  return response.data;
}

/** `POST /claims/{id}/reject` — incelemedeki hasarı reddeder (gerekçe zorunlu; personel). */
export async function rejectClaim(
  id: string,
  request: RejectClaimRequest,
): Promise<ClaimSummary> {
  const response = await api.post<ClaimSummary>(`/claims/${id}/reject`, request);
  return response.data;
}

/** `POST /claims/{id}/pay` — onaylanmış hasarın ödemesini gerçekleştirir (Approved → Paid; personel). */
export async function payClaim(id: string): Promise<ClaimSummary> {
  const response = await api.post<ClaimSummary>(`/claims/${id}/pay`);
  return response.data;
}

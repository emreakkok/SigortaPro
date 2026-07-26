import type { ClaimStatus } from "@/shared/types/insurance.types";

/** Hasara eklenen belge/görsel metadata'sı (backend `ClaimDocumentDto`). İçerik ayrı uçla indirilir. */
export interface ClaimDocument {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  isImage: boolean;
  createdAt: string;
}

/** Hasar detayı (backend `ClaimDto`). */
export interface Claim {
  id: string;
  policyId: string;
  policyNumber: string;
  customerId: string;
  incidentDate: string;
  description: string;
  estimatedAmount: number;
  approvedAmount: number | null;
  status: ClaimStatus;
  reviewNote: string | null;
  createdAt: string;
  documents: ClaimDocument[];
}

/** Hasar listesi özeti (backend `ClaimSummaryDto`). */
export interface ClaimSummary {
  id: string;
  policyId: string;
  policyNumber: string;
  status: ClaimStatus;
  incidentDate: string;
  estimatedAmount: number;
  approvedAmount: number | null;
  createdAt: string;
}

/** Hasar bildiriminde yüklenen belge (içerik base64 — backend `CreateClaimDocument`, byte[] olarak çözer). */
export interface CreateClaimDocumentPayload {
  fileName: string;
  contentType: string;
  content: string;
}

/** `POST /claims` istek gövdesi (backend `CreateClaimCommand`). */
export interface CreateClaimRequest {
  policyId: string;
  incidentDate: string;
  description: string;
  estimatedAmount: number;
  documents?: CreateClaimDocumentPayload[];
}

/** `GET /claims` sorgu parametreleri. */
export interface ClaimListParams {
  page?: number;
  pageSize?: number;
  status?: ClaimStatus;
  policyId?: string;
}

/** `POST /claims/{id}/approve` istek gövdesi (backend `ApproveClaimCommand` — ClaimId route'tan). */
export interface ApproveClaimRequest {
  approvedAmount: number;
  reviewNote?: string;
}

/** `POST /claims/{id}/reject` istek gövdesi (backend `RejectClaimCommand` — ClaimId route'tan). */
export interface RejectClaimRequest {
  reviewNote: string;
}

import type { ClaimStatus } from "@/shared/types/insurance.types";

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

/** `POST /claims` istek gövdesi (backend `CreateClaimCommand`). */
export interface CreateClaimRequest {
  policyId: string;
  incidentDate: string;
  description: string;
  estimatedAmount: number;
  photoFileNames?: string[];
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

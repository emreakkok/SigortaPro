import type { InsuranceBranch, QuoteStatus } from "@/shared/types/insurance.types";

/** Müşteriye sunulan yenileme teklifi (backend `RenewalDto`). */
export interface Renewal {
  id: string;
  policyId: string;
  policyNumber: string;
  newQuoteId: string;
  branch: InsuranceBranch;
  newQuoteStatus: QuoteStatus;
  offeredPremium: number;
  validUntil: string | null;
  offeredAt: string;
  isAccepted: boolean;
  acceptedAt: string | null;
}

/** `GET /renewals` sorgu parametreleri. */
export interface RenewalListParams {
  page?: number;
  pageSize?: number;
}

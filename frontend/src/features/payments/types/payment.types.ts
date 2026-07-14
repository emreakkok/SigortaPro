import type { PaymentStatus, PolicyStatus } from "@/shared/types/insurance.types";

/** Taksit seçeneği (backend `InstallmentOptionDto`). Faizsiz mock: totalAmount == prim. */
export interface InstallmentOption {
  count: number;
  monthlyAmount: number;
  totalAmount: number;
}

/** `POST /payments` istek gövdesi (backend `PurchaseQuoteCommand`). */
export interface PurchaseQuoteRequest {
  quoteId: string;
  cardNumber: string;
  cardHolderName: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
  installmentCount: number;
}

/** Satın alma sonucu poliçe özeti (backend `PolicySummaryDto`). */
export interface PurchasePolicySummary {
  id: string;
  policyNumber: string;
  status: PolicyStatus;
  startDate: string;
  endDate: string;
  totalPremium: number;
}

/** `POST /payments` yanıtı (backend `PurchaseResultDto`). Kart yalnızca maskeli döner. */
export interface PurchaseResult {
  paymentId: string;
  paymentStatus: PaymentStatus;
  maskedCardNumber: string;
  amount: number;
  installmentCount: number;
  providerReferenceCode: string | null;
  policy: PurchasePolicySummary;
}

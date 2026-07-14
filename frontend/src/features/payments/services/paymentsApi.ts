import { api } from "@/shared/lib/axios";
import type {
  InstallmentOption,
  PurchaseQuoteRequest,
  PurchaseResult,
} from "@/features/payments/types/payment.types";

/** `GET /payments/installment-options?quoteId=` — onaylanmış teklifin taksit seçenekleri. */
export async function getInstallmentOptions(quoteId: string): Promise<InstallmentOption[]> {
  const response = await api.get<InstallmentOption[]>("/payments/installment-options", {
    params: { quoteId },
  });
  return response.data;
}

/** `POST /payments` — mock sanal POS ile ödeme alır; başarılıysa poliçe oluşur (Purchased). */
export async function purchaseQuote(request: PurchaseQuoteRequest): Promise<PurchaseResult> {
  const response = await api.post<PurchaseResult>("/payments", request);
  return response.data;
}

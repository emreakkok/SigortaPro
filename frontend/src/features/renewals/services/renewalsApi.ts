import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  Renewal,
  RenewalListParams,
} from "@/features/renewals/types/renewal.types";

/** `GET /renewals` — oturum sahibi müşterinin yenileme teklifleri (sayfalı). */
export async function getMyRenewals(
  params: RenewalListParams,
): Promise<PagedResult<Renewal>> {
  const response = await api.get<PagedResult<Renewal>>("/renewals", { params });
  return response.data;
}

/** `POST /renewals/{id}/accept` — yenilemeyi onaylar; yeni dönem teklifi Approved olur (ödemeye hazır). */
export async function acceptRenewal(id: string): Promise<Renewal> {
  const response = await api.post<Renewal>(`/renewals/${id}/accept`);
  return response.data;
}

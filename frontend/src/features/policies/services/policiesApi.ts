import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  PolicyDetail,
  PolicyListItem,
  PolicyListParams,
} from "@/features/policies/types/policy.types";

/** `GET /policies` — oturum sahibi müşterinin poliçeleri (sayfalı, opsiyonel durum filtresi). */
export async function getMyPolicies(
  params: PolicyListParams,
): Promise<PagedResult<PolicyListItem>> {
  const response = await api.get<PagedResult<PolicyListItem>>("/policies", { params });
  return response.data;
}

/** `GET /policies/{id}` — poliçe detayı (teminat tablosu ile). */
export async function getPolicyById(id: string): Promise<PolicyDetail> {
  const response = await api.get<PolicyDetail>(`/policies/${id}`);
  return response.data;
}

/** Content-Disposition başlığından dosya adını çıkarır; yoksa poliçe numarasından üretir. */
function fileNameFromDisposition(disposition: string | undefined, fallback: string): string {
  if (disposition === undefined) {
    return fallback;
  }
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  return match?.[1] !== undefined ? decodeURIComponent(match[1]) : fallback;
}

/**
 * `GET /policies/{id}/document` — poliçe PDF'ini indirir ve tarayıcıda kaydettirir.
 * Bearer başlığı axios interceptor'ında eklendiğinden basit bir `<a href>` yerine blob indirilir.
 */
export async function downloadPolicyDocument(
  policyId: string,
  policyNumber: string,
): Promise<void> {
  const response = await api.get<Blob>(`/policies/${policyId}/document`, {
    responseType: "blob",
  });

  const fileName = fileNameFromDisposition(
    response.headers["content-disposition"] as string | undefined,
    `${policyNumber}.pdf`,
  );

  const url = URL.createObjectURL(response.data);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

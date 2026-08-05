import { api } from "@/shared/lib/axios";
import type {
  CreatePricingDraftRequest,
  PricingVersion,
  UpdatePricingDraftRequest,
} from "@/features/pricing/types/pricing.types";

/** Yürürlükteki tarife + taslak + geçmiş (ADR-048). Okuma acente personeline açıktır. */
export async function getPricingVersions(): Promise<PricingVersion[]> {
  const response = await api.get<PricingVersion[]>("/pricing/versions");
  return response.data;
}

/** Yeni TASLAK versiyon oluşturur (isim zorunlu; aktif tarifeden seed edilir). Yalnızca Admin. */
export async function createPricingDraft(request: CreatePricingDraftRequest): Promise<PricingVersion> {
  const response = await api.post<PricingVersion>("/pricing/versions", request);
  return response.data;
}

/** TASLAK versiyonu düzenler (baz primler + paket/şehir/yenileme kaldıraçları). Yalnızca Admin. */
export async function updatePricingDraft(
  id: string,
  request: UpdatePricingDraftRequest,
): Promise<PricingVersion> {
  const response = await api.put<PricingVersion>(`/pricing/versions/${id}`, request);
  return response.data;
}

/** TASLAK versiyonu aktifleştirir (bundan sonraki teklifler yeni tarifeyi kullanır). Yalnızca Admin. */
export async function activatePricingVersion(id: string): Promise<PricingVersion> {
  const response = await api.post<PricingVersion>(`/pricing/versions/${id}/activate`);
  return response.data;
}

/** Kullanılmayan TASLAK versiyonu iptal eder (soft-delete). Yalnızca Admin. */
export async function discardPricingDraft(id: string): Promise<void> {
  await api.delete(`/pricing/versions/${id}`);
}

import { api } from "@/shared/lib/axios";
import type {
  CreatePricingVersionRequest,
  PricingVersion,
} from "@/features/pricing/types/pricing.types";

/** Fiyatlandırma yönetimi uçları (ADR-048) — yalnızca Admin yetkisiyle erişilebilir. */
export async function getPricingVersions(): Promise<PricingVersion[]> {
  const response = await api.get<PricingVersion[]>("/pricing/versions");
  return response.data;
}

export async function createPricingVersion(
  request: CreatePricingVersionRequest,
): Promise<PricingVersion> {
  const response = await api.post<PricingVersion>("/pricing/versions", request);
  return response.data;
}

import { api } from "@/shared/lib/axios";
import type { CityCatalog } from "@/shared/types/cityCatalog.types";

/** `GET /city-catalog` — Türkiye'nin 81 ili (salt referans veri, ADR-037). */
export async function getCityCatalog(): Promise<CityCatalog> {
  const response = await api.get<CityCatalog>("/city-catalog");
  return response.data;
}

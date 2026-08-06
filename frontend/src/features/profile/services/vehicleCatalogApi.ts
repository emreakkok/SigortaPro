import { api } from "@/shared/lib/axios";
import type { VehicleCatalog } from "@/features/profile/types/vehicleCatalog.types";

/** `GET /vehicle-catalog` — araç marka/model kataloğu (salt referans veri). */
export async function getVehicleCatalog(): Promise<VehicleCatalog> {
  const response = await api.get<VehicleCatalog>("/vehicle-catalog");
  return response.data;
}

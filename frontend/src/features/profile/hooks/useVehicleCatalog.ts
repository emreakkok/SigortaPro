import { useQuery } from "@tanstack/react-query";
import { getVehicleCatalog } from "@/features/profile/services/vehicleCatalogApi";

export const vehicleCatalogQueryKey = ["vehicle-catalog"] as const;

/**
 * Araç kataloğu sorgusu. Katalog statik referans veridir (oturum boyunca değişmez) → staleTime Infinity;
 * backend zaten In-Memory cache'ler, tek çağrı yeterlidir.
 */
export function useVehicleCatalog() {
  return useQuery({
    queryKey: vehicleCatalogQueryKey,
    queryFn: getVehicleCatalog,
    staleTime: Number.POSITIVE_INFINITY,
  });
}

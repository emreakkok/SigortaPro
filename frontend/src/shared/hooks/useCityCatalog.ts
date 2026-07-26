import { useQuery } from "@tanstack/react-query";
import { getCityCatalog } from "@/shared/lib/cityCatalogApi";

export const cityCatalogQueryKey = ["city-catalog"] as const;

/**
 * İl kataloğu sorgusu. 81 il statik referans veridir (oturum boyunca değişmez) → staleTime Infinity;
 * backend zaten In-Memory cache'ler (ADR-037), tek çağrı yeterlidir. Birden fazla adres formu aynı
 * cache'i paylaşır.
 */
export function useCityCatalog() {
  return useQuery({
    queryKey: cityCatalogQueryKey,
    queryFn: getCityCatalog,
    staleTime: Number.POSITIVE_INFINITY,
  });
}

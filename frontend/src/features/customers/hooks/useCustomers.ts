import { useQuery } from "@tanstack/react-query";
import {
  getCustomerById,
  getCustomers,
} from "@/features/customers/services/customersApi";
import type { CustomerListParams } from "@/features/customers/types/customer.types";

export const customersQueryKeys = {
  all: ["customers"] as const,
  list: (params: CustomerListParams) => ["customers", "list", params] as const,
  detail: (id: string) => ["customers", "detail", id] as const,
};

/** Müşteri listesi (sayfalı; ad/soyad/TCKN araması + il filtresi). */
export function useCustomerList(params: CustomerListParams) {
  return useQuery({
    queryKey: customersQueryKeys.list(params),
    queryFn: () => getCustomers(params),
  });
}

/** Müşteri profil detayı; çekmece kapalıyken (`id === null`) sorgu çalışmaz. */
export function useCustomer(id: string | null) {
  return useQuery({
    queryKey: customersQueryKeys.detail(id ?? ""),
    queryFn: () => getCustomerById(id ?? ""),
    enabled: id !== null,
  });
}

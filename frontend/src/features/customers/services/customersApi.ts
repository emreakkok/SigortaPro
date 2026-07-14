import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  CustomerListParams,
  CustomerSummary,
} from "@/features/customers/types/customer.types";
import type { CustomerProfile } from "@/features/profile/types/profile.types";

/** `GET /customers` — müşteri listesi (acente personeli; sayfalama + arama + il filtresi). */
export async function getCustomers(
  params: CustomerListParams,
): Promise<PagedResult<CustomerSummary>> {
  const response = await api.get<PagedResult<CustomerSummary>>("/customers", { params });
  return response.data;
}

/**
 * `GET /customers/{id}` — müşteri profil detayı (acente personeli). Backend `CustomerDto`,
 * profil ekranının tükettiği tiple aynıdır → `CustomerProfile` yeniden kullanılır (DRY).
 */
export async function getCustomerById(id: string): Promise<CustomerProfile> {
  const response = await api.get<CustomerProfile>(`/customers/${id}`);
  return response.data;
}

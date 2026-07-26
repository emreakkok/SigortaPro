import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  CreateStaffRequest,
  SetStaffStatusRequest,
  StaffDetail,
  StaffListItem,
  StaffListParams,
  UpdateStaffRequest,
} from "@/features/staff/types/staff.types";

/**
 * Personel yönetimi uçları (ADR-060) — tümü backend'de yalnızca Admin yetkisiyle erişilebilir.
 * Mevcut tek Axios instance'ı kullanılır (interceptor'lar 401 yenileme + hata zarfını yönetir).
 */

/** `GET /staff` — personel listesi (sayfalı; e-posta/ad araması + aktiflik filtresi). */
export async function getStaffList(params: StaffListParams): Promise<PagedResult<StaffListItem>> {
  const response = await api.get<PagedResult<StaffListItem>>("/staff", { params });
  return response.data;
}

/** `GET /staff/{id}` — personel detayı (hedef personel değilse backend 404 döner). */
export async function getStaffById(id: string): Promise<StaffDetail> {
  const response = await api.get<StaffDetail>(`/staff/${id}`);
  return response.data;
}

/**
 * `POST /staff` — yeni Personel hesabı. Rol backend'de sabittir; istekte rol gönderilmez.
 * Sözleşme birebir: yalnızca `{ email, fullName, password }`.
 */
export async function createStaff(request: CreateStaffRequest): Promise<StaffDetail> {
  const response = await api.post<StaffDetail>("/staff", request);
  return response.data;
}

/** `PUT /staff/{id}` — personelin görünen adını günceller (e-posta/rol değişmez). */
export async function updateStaff(id: string, request: UpdateStaffRequest): Promise<StaffDetail> {
  const response = await api.put<StaffDetail>(`/staff/${id}`, request);
  return response.data;
}

/** `PATCH /staff/{id}/status` — personeli aktif/pasif yapar (204; gövde döndürmez). */
export async function setStaffStatus(id: string, request: SetStaffStatusRequest): Promise<void> {
  await api.patch(`/staff/${id}/status`, request);
}

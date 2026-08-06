import type { PaginationParams } from "@/shared/types/api.types";

/** Personel liste satırı (backend `StaffListItemDto`). Hassas alan taşımaz. */
export interface StaffListItem {
  id: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
}

/** Personel detayı (backend `StaffDetailDto`). Roller daima `["Personel"]`'dir. */
export interface StaffDetail {
  id: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
  roles: string[];
}

/** `GET /staff` sorgu parametreleri (sayfalama + e-posta/ad araması + aktiflik filtresi). */
export interface StaffListParams extends PaginationParams {
  searchTerm?: string;
  isActive?: boolean;
}

/**
 * `POST /staff` istek gövdesi. GÜVENLİK: `role`/`isActive` alanı YOKTUR — rol backend'de daima
 * `Personel`'e sabitlenir. Admin oluşturma yolu yoktur.
 */
export interface CreateStaffRequest {
  email: string;
  fullName: string;
  password: string;
}

/** `PUT /staff/{id}` istek gövdesi — yalnızca görünen ad güncellenebilir. */
export interface UpdateStaffRequest {
  fullName: string;
}

/** `PATCH /staff/{id}/status` istek gövdesi. */
export interface SetStaffStatusRequest {
  isActive: boolean;
}

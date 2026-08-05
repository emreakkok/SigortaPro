import { api } from "@/shared/lib/axios";
import type {
  CustomerProfile,
  PropertyRequest,
  Property,
  UpdateProfileRequest,
  Vehicle,
  VehicleRequest,
} from "@/features/profile/types/profile.types";

/** `GET /customers/me` — oturum sahibinin profili (araç/konut ile). */
export async function getMyProfile(): Promise<CustomerProfile> {
  const response = await api.get<CustomerProfile>("/customers/me");
  return response.data;
}

/** `PUT /customers/me` — ad/soyad, telefon ve adres günceller. */
export async function updateProfile(request: UpdateProfileRequest): Promise<CustomerProfile> {
  const response = await api.put<CustomerProfile>("/customers/me", request);
  return response.data;
}

/**
 * `POST /customers/me/vehicles` — yeni araç ekler.
 * customerId verilirse acente destekli: `POST /customers/{customerId}/vehicles` — personel, müşteri adına
 * teklif hazırlarken aracı müşterinin profiline ekler.
 */
export async function addVehicle(request: VehicleRequest, customerId?: string): Promise<Vehicle> {
  const url = customerId ? `/customers/${customerId}/vehicles` : "/customers/me/vehicles";
  const response = await api.post<Vehicle>(url, request);
  return response.data;
}

/** `PUT /customers/me/vehicles/{id}` — mevcut aracı günceller (sahiplik kontrollü). */
export async function updateVehicle(id: string, request: VehicleRequest): Promise<Vehicle> {
  const response = await api.put<Vehicle>(`/customers/me/vehicles/${id}`, request);
  return response.data;
}

/**
 * `POST /customers/me/properties` — yeni konut ekler.
 * customerId verilirse acente destekli: `POST /customers/{customerId}/properties`.
 */
export async function addProperty(request: PropertyRequest, customerId?: string): Promise<Property> {
  const url = customerId ? `/customers/${customerId}/properties` : "/customers/me/properties";
  const response = await api.post<Property>(url, request);
  return response.data;
}

/**
 * `POST /auth/change-password` — oturum sahibinin şifresini değiştirir (ADR-040).
 * Hesap öz-yönetimi profil yüzeyinin parçası olduğundan bu feature'da yaşar
 * (auth → profile bağımlılığı zaten var — ters import feature döngüsü yaratırdı).
 * Mevcut şifre hatalıysa backend 400 `{ errors }` döner.
 */
export async function changePassword(request: {
  currentPassword: string;
  newPassword: string;
}): Promise<void> {
  await api.post("/auth/change-password", request);
}

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

/** `POST /customers/me/vehicles` — yeni araç ekler. */
export async function addVehicle(request: VehicleRequest): Promise<Vehicle> {
  const response = await api.post<Vehicle>("/customers/me/vehicles", request);
  return response.data;
}

/** `PUT /customers/me/vehicles/{id}` — mevcut aracı günceller (sahiplik kontrollü). */
export async function updateVehicle(id: string, request: VehicleRequest): Promise<Vehicle> {
  const response = await api.put<Vehicle>(`/customers/me/vehicles/${id}`, request);
  return response.data;
}

/** `POST /customers/me/properties` — yeni konut ekler. */
export async function addProperty(request: PropertyRequest): Promise<Property> {
  const response = await api.post<Property>("/customers/me/properties", request);
  return response.data;
}

import type { VehicleUsage } from "@/shared/types/insurance.types";

/** Adres value object (backend `AddressDto`). */
export interface Address {
  city: string;
  district: string;
  neighborhood: string;
  postalCode: string;
}

/** Araç risk objesi (backend `VehicleDto`). */
export interface Vehicle {
  id: string;
  plateNumber: string;
  brand: string;
  model: string;
  manufactureYear: number;
  enginePowerHp: number;
  /** Kullanım amacı beyanı (ADR-057); bu alan eklenmeden kaydedilmiş araçlarda null. */
  usagePurpose: VehicleUsage | null;
}

/** Konut risk objesi (backend `PropertyDto`). */
export interface Property {
  id: string;
  address: Address;
  buildingAge: number;
  squareMeters: number;
  /**
   * Kayıt anında belirlenen deprem bölgesi. ADR-055'ten itibaren sistem tarafından adresin ilinden
   * türetilir (kullanıcı seçemez). Daha eski kayıtlarda müşterinin o günkü beyanıdır ve tarihsel
   * doğruluk için korunur (ADR-058). Salt okunurdur.
   */
  earthquakeZone: number;
}

/** Müşteri profil detayı (backend `CustomerDto`) — ham TCKN taşımaz, maskeli döner. */
export interface CustomerProfile {
  id: string;
  firstName: string;
  lastName: string;
  maskedTckn: string;
  birthDate: string;
  phoneNumber: string;
  email: string | null;
  address: Address;
  vehicles: Vehicle[];
  properties: Property[];
}

/** `PUT /customers/me` istek gövdesi (backend `UpdateProfileCommand`). */
export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  city: string;
  district: string;
  neighborhood: string;
  postalCode: string;
}

/** `POST /customers/me/vehicles` istek gövdesi (backend `AddVehicleCommand`). */
export interface VehicleRequest {
  plateNumber: string;
  brand: string;
  model: string;
  manufactureYear: number;
  enginePowerHp: number;
  /** Kullanım amacı beyanı (ADR-057) — zorunlu; Kasko/Trafik primini etkiler. */
  usagePurpose: VehicleUsage;
}

/** `POST /customers/me/properties` istek gövdesi (backend `AddPropertyCommand`). */
export interface PropertyRequest {
  city: string;
  district: string;
  neighborhood: string;
  postalCode: string;
  buildingAge: number;
  squareMeters: number;
}

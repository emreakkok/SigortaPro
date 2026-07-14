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
}

/** Konut risk objesi (backend `PropertyDto`). */
export interface Property {
  id: string;
  address: Address;
  buildingAge: number;
  squareMeters: number;
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
}

/** `POST /customers/me/properties` istek gövdesi (backend `AddPropertyCommand`). */
export interface PropertyRequest {
  city: string;
  district: string;
  neighborhood: string;
  postalCode: string;
  buildingAge: number;
  squareMeters: number;
  earthquakeZone: number;
}

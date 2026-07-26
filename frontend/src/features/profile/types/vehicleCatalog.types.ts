/** `GET /vehicle-catalog` yanıtı (backend `VehicleCatalogDto`). Cascading select verisi. */
export interface VehicleBrand {
  name: string;
  models: string[];
}

export interface VehicleCatalog {
  brands: VehicleBrand[];
}

/** `GET /city-catalog` yanıtı (backend `CityCatalogDto`). Adres formu il combobox verisi. */
export interface City {
  name: string;
}

export interface CityCatalog {
  cities: City[];
}

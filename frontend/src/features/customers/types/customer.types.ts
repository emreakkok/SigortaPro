/** Admin müşteri listesi kalemi (backend `CustomerSummaryDto` — TCKN maskeli). */
export interface CustomerSummary {
  id: string;
  firstName: string;
  lastName: string;
  maskedTckn: string;
  phoneNumber: string;
  city: string;
  createdAt: string;
}

/** `GET /customers` sorgu parametreleri (ad/soyad/TCKN araması + il filtresi). */
export interface CustomerListParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  city?: string;
}

/** Backend'in RFC 7807 hata zarfı (ExceptionHandlingMiddleware — ADR-018). */
export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Alan bazlı doğrulama hataları (ValidationException → 400). */
  errors?: Record<string, string[]>;
  traceId?: string;
  correlationId?: string;
}

/** Backend `PagedResult<T>` sarmalayıcısının JSON karşılığı. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Listeleme uçlarının ortak sayfalama parametreleri (varsayılan pageSize 20, maks. 100). */
export interface PaginationParams {
  page?: number;
  pageSize?: number;
}

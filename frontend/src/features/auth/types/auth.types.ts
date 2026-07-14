/** `POST /auth/login` istek gövdesi (backend `LoginCommand`). */
export interface LoginRequest {
  email: string;
  password: string;
}

/** `POST /auth/register` istek gövdesi (backend `RegisterCommand` ile birebir). */
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  tckn: string;
  /** ISO tarih (input type="date" → "YYYY-MM-DD"); backend DateTime'a bağlar. */
  birthDate: string;
  phoneNumber: string;
  city: string;
  district: string;
  neighborhood: string;
  postalCode: string;
}

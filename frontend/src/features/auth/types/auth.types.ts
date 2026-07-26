/** `POST /auth/login` istek gövdesi (backend `LoginCommand`). */
export interface LoginRequest {
  email: string;
  password: string;
}

/** `POST /auth/forgot-password` istek gövdesi (backend `ForgotPasswordCommand`). */
export interface ForgotPasswordRequest {
  email: string;
}

/** `POST /auth/reset-password` istek gövdesi (backend `ResetPasswordCommand`). */
export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
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

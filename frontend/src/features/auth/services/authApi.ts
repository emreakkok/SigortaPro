import { api } from "@/shared/lib/axios";
import type { AuthResponse } from "@/shared/types/auth.types";
import type {
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
} from "@/features/auth/types/auth.types";

/**
 * Auth uçları anonimdir; `api` instance'ı kullanılır ancak bu path'ler 401 refresh
 * döngüsünden muaftır (axios.ts AUTH_ENDPOINTS). Backend login/register soft-fail'de
 * `{ errors: string[] }` döner — mesajlar `getApiErrorMessages` ile ayrıştırılır.
 */
export async function login(request: LoginRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/login", request);
  return response.data;
}

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/auth/register", request);
  return response.data;
}

/**
 * Şifre sıfırlama talebi. Backend güvenlik gereği (kullanıcı varlığını sızdırmama) her zaman
 * 200 döner; bu yüzden başarı, e-postanın kayıtlı olduğu anlamına gelmez.
 */
export async function forgotPassword(request: ForgotPasswordRequest): Promise<void> {
  await api.post("/auth/forgot-password", request);
}

/** Sıfırlama token'ı + yeni şifre ile şifreyi günceller. Geçersiz/süresi dolmuş token → 400. */
export async function resetPassword(request: ResetPasswordRequest): Promise<void> {
  await api.post("/auth/reset-password", request);
}

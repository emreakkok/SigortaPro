import { api } from "@/shared/lib/axios";
import type { AuthResponse } from "@/shared/types/auth.types";
import type { LoginRequest, RegisterRequest } from "@/features/auth/types/auth.types";

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

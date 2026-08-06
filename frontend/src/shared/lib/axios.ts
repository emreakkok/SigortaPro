import axios, { AxiosError, type AxiosRequestConfig } from "axios";
import { env } from "@/shared/lib/env";
import { clearSession, getSession, setSession } from "@/shared/lib/session";
import type { ApiProblemDetails } from "@/shared/types/api.types";
import type { AuthResponse } from "@/shared/types/auth.types";

/**
 * Uygulama genelinde kullanılan tek Axios instance'ı:
 * - İstek interceptor'ı oturumdaki access token'ı `Authorization: Bearer` olarak ekler.
 * - Yanıt interceptor'ı 401'de refresh token ile oturumu yeniler (tek uçuş / single-flight),
 *   bekleyen isteği yeni token'la bir kez tekrarlar; yenileme de başarısızsa oturumu
 *   temizleyip login'e yönlendirir.
 */
export const api = axios.create({
  baseURL: env.apiBaseUrl,
  headers: {
    "Content-Type": "application/json",
  },
});

/** Kimlik uçları: 401/400 dönseler bile token yenileme döngüsüne girmemeli (hepsi anonim). */
const AUTH_ENDPOINTS = [
  "/auth/login",
  "/auth/register",
  "/auth/refresh-token",
  "/auth/forgot-password",
  "/auth/reset-password",
];

interface RetriableRequestConfig extends AxiosRequestConfig {
  /** Bu istek refresh sonrası bir kez tekrarlandı mı? (sonsuz döngü koruması) */
  _retried?: boolean;
}

/** Eşzamanlı 401'lerde tek refresh isteği atılır; diğerleri aynı sözü (promise) bekler. */
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const session = getSession();
  if (session === null) {
    return null;
  }

  try {
    // Ham axios kullanılır: instance interceptor'ı tetiklenmemeli.
    const response = await axios.post<AuthResponse>(
      `${env.apiBaseUrl}/auth/refresh-token`,
      { refreshToken: session.refreshToken },
      { headers: { "Content-Type": "application/json" } },
    );

    const auth = response.data;
    setSession({
      userId: auth.userId,
      email: auth.email,
      roles: auth.roles,
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
    });
    return auth.accessToken;
  } catch {
    return null;
  }
}

api.interceptors.request.use((config) => {
  const session = getSession();
  if (session !== null) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiProblemDetails>) => {
    const config = error.config as RetriableRequestConfig | undefined;
    const isAuthEndpoint = AUTH_ENDPOINTS.some((path) => config?.url?.includes(path));

    if (
      error.response?.status !== 401 ||
      config === undefined ||
      config._retried === true ||
      isAuthEndpoint ||
      getSession() === null
    ) {
      return Promise.reject(error);
    }

    refreshPromise ??= refreshAccessToken().finally(() => {
      refreshPromise = null;
    });
    const newAccessToken = await refreshPromise;

    if (newAccessToken === null) {
      clearSession();
      // Router dışından tam sayfa yönlendirme (interceptor React ağacında yaşamaz);
      // yeniden yükleme AuthProvider state'ini de sıfırlar. 401 sayfası
      // kullanıcıya oturumun neden düştüğünü açıklar.
      window.location.assign("/401");
      return Promise.reject(error);
    }

    config._retried = true;
    config.headers = { ...config.headers, Authorization: `Bearer ${newAccessToken}` };
    return api.request(config);
  },
);

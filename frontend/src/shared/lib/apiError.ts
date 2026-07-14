import { isAxiosError } from "axios";
import type { ApiProblemDetails } from "@/shared/types/api.types";

/**
 * Backend iki hata gövdesi üretir:
 * 1. RFC 7807 ProblemDetails (`errors`: alan → mesaj[] sözlüğü) — middleware (ADR-018).
 * 2. Auth soft-fail zarfı `{ errors: string[] }` — login 401 / register 409 (Task 5).
 * Bu yardımcı ikisini de kullanıcıya gösterilecek düz mesaj listesine indirger.
 */
export function getApiErrorMessages(error: unknown): string[] {
  if (!isAxiosError(error)) {
    return ["Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin."];
  }

  if (error.response === undefined) {
    return ["Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin."];
  }

  const data = error.response.data as
    | ApiProblemDetails
    | { errors?: string[] }
    | null
    | undefined;

  if (typeof data === "object" && data !== null && Array.isArray(data.errors)) {
    return data.errors.length > 0 ? data.errors : [defaultMessageFor(error.response.status)];
  }

  const problem = data as ApiProblemDetails | null | undefined;
  if (problem?.errors !== undefined && !Array.isArray(problem.errors)) {
    const fieldMessages = Object.values(problem.errors).flat();
    if (fieldMessages.length > 0) {
      return fieldMessages;
    }
  }

  if (typeof problem?.detail === "string" && problem.detail.length > 0) {
    return [problem.detail];
  }

  return [defaultMessageFor(error.response.status)];
}

function defaultMessageFor(status: number): string {
  switch (status) {
    case 401:
      return "Kimlik doğrulama başarısız.";
    case 403:
      return "Bu işlem için yetkiniz yok.";
    case 404:
      return "Kayıt bulunamadı.";
    case 429:
      return "Çok fazla deneme yapıldı. Lütfen bir dakika sonra tekrar deneyin.";
    default:
      return "Bir hata oluştu. Lütfen tekrar deneyin.";
  }
}

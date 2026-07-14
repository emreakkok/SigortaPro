import type { AuthSession, UserRole } from "@/shared/types/auth.types";
import { STAFF_ROLES } from "@/shared/types/auth.types";

/**
 * Oturum saklama katmanı (ADR-028): oturum localStorage'da tek anahtar altında tutulur
 * ve kalıcı doğruluk kaynağıdır. Axios interceptor'ları buradan okur/yazar; React
 * tarafındaki yansıması `features/auth` içindeki AuthProvider'dır (ADR-029).
 */
const SESSION_STORAGE_KEY = "sigortapro.session";

export function getSession(): AuthSession | null {
  const raw = localStorage.getItem(SESSION_STORAGE_KEY);
  if (raw === null) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    // Bozuk kayıt oturumsuz durumla eşdeğerdir; temizle ve oturumsuz davran.
    localStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }
}

export function setSession(session: AuthSession): void {
  localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  localStorage.removeItem(SESSION_STORAGE_KEY);
}

export function hasAnyRole(session: AuthSession, roles: readonly UserRole[]): boolean {
  return session.roles.some((role) => roles.includes(role));
}

export function isStaff(session: AuthSession): boolean {
  return hasAnyRole(session, STAFF_ROLES);
}

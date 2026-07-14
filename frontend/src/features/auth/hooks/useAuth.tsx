import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { clearSession, getSession, isStaff, setSession } from "@/shared/lib/session";
import type { AuthResponse, AuthSession } from "@/shared/types/auth.types";

interface AuthContextValue {
  /** Aktif oturum; yoksa null. */
  session: AuthSession | null;
  /** Login/register yanıtını oturuma çevirir: localStorage + React state (ADR-029). */
  signIn: (auth: AuthResponse) => AuthSession;
  /** Oturumu sonlandırır (çıkış); yönlendirme çağıranın sorumluluğundadır. */
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Oturum state'inin React tarafı (ADR-029): localStorage (session.ts) kalıcı doğruluk
 * kaynağıdır, bu provider onu React state olarak yansıtır. Axios'un sessiz token
 * yenilemesi yalnızca localStorage'ı günceller — state'teki token'lar bayatlayabilir
 * ancak UI token okumaz (istekler interceptor'da localStorage'dan okur); zorunlu
 * çıkışta (refresh başarısız) tam sayfa yönlendirme yapıldığından state sıfırlanır.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState<AuthSession | null>(() => getSession());

  const signIn = useCallback((auth: AuthResponse): AuthSession => {
    const newSession: AuthSession = {
      userId: auth.userId,
      email: auth.email,
      roles: auth.roles,
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
    };
    setSession(newSession);
    setSessionState(newSession);
    return newSession;
  }, []);

  const signOut = useCallback(() => {
    clearSession();
    setSessionState(null);
  }, []);

  const value = useMemo(() => ({ session, signIn, signOut }), [session, signIn, signOut]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (context === null) {
    throw new Error("useAuth yalnızca AuthProvider altında kullanılabilir.");
  }
  return context;
}

/** Oturumdaki role göre kullanıcının ana sayfası. */
export function homePathFor(session: AuthSession): string {
  return isStaff(session) ? "/admin" : "/portal";
}

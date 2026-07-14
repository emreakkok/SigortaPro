import { useMutation } from "@tanstack/react-query";
import { useLocation, useNavigate } from "react-router-dom";
import { homePathFor, useAuth } from "@/features/auth/hooks/useAuth";
import { login } from "@/features/auth/services/authApi";
import type { LoginRequest } from "@/features/auth/types/auth.types";
import { hasAnyRole } from "@/shared/lib/session";
import type { AuthSession } from "@/shared/types/auth.types";
import { UserRoles } from "@/shared/types/auth.types";

interface RedirectState {
  from?: { pathname?: string };
}

/** Giriş mutation'ı: başarıda oturumu açar ve role göre (veya geldiği sayfaya) yönlendirir. */
export function useLogin() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  return useMutation({
    mutationFn: (request: LoginRequest) => login(request),
    onSuccess: (auth) => {
      const session = signIn(auth);
      navigate(resolveDestination(session, location.state as RedirectState | null), {
        replace: true,
      });
    },
  });
}

/**
 * Login sonrası hedef: guard'ın bıraktığı `from` adresi, oturumun rolüyle uyumluysa
 * kullanılır (Customer → /portal*, Staff → /admin*); değilse rolün ana sayfası.
 */
function resolveDestination(session: AuthSession, state: RedirectState | null): string {
  const from = state?.from?.pathname;
  if (from !== undefined) {
    const isCustomerArea = from.startsWith("/portal");
    const isStaffArea = from.startsWith("/admin");
    if (isCustomerArea && hasAnyRole(session, [UserRoles.Customer])) {
      return from;
    }
    if (isStaffArea && hasAnyRole(session, [UserRoles.Admin, UserRoles.Personel])) {
      return from;
    }
  }
  return homePathFor(session);
}

import { useAuth } from "@/features/auth/hooks/useAuth";
import { isAdmin, isCustomerRole, isStaff } from "@/shared/lib/session";

/**
 * Aktif oturumun rol bayrakları. Tek merkezden türetilir; navigasyon görünürlüğü ve
 * koşullu aksiyon render'ı bunu kullanır. `useAuth().session`'a bağlı olduğundan oturum değişiminde
 * (login/logout/refresh) yeniden render tetikler — localStorage'ı doğrudan okuyan eski desenin aksine.
 *
 * NOT: Bu yalnızca UX/navigasyon kontrolüdür; gerçek yetkilendirme her zaman backend'dedir.
 */
export function useRoles(): { isAdmin: boolean; isStaff: boolean; isCustomer: boolean } {
  const { session } = useAuth();

  if (session === null) {
    return { isAdmin: false, isStaff: false, isCustomer: false };
  }

  return {
    isAdmin: isAdmin(session),
    isStaff: isStaff(session),
    isCustomer: isCustomerRole(session),
  };
}

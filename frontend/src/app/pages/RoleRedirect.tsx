import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { homePathFor, useAuth } from "@/features/auth/hooks/useAuth";

/**
 * Kök adres ("/"): oturum varsa role göre portal/panele yönlendirir; oturum yoksa
 * karşılama (landing) sayfasını gösterir.
 */
export function RoleRedirect({ landing }: { landing: ReactNode }) {
  const { session } = useAuth();

  if (session === null) {
    return <>{landing}</>;
  }

  return <Navigate to={homePathFor(session)} replace />;
}

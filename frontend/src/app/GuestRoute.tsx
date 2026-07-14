import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { homePathFor, useAuth } from "@/features/auth/hooks/useAuth";

/** Yalnızca oturumsuz erişim (login/register): oturum varsa rolün ana sayfasına gönderir. */
export function GuestRoute({ children }: { children: ReactNode }) {
  const { session } = useAuth();

  if (session !== null) {
    return <Navigate to={homePathFor(session)} replace />;
  }

  return <>{children}</>;
}

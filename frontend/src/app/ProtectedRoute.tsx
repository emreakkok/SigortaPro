import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { hasAnyRole } from "@/shared/lib/session";
import type { UserRole } from "@/shared/types/auth.types";

interface ProtectedRouteProps {
  /** Boş bırakılırsa yalnızca oturum varlığı aranır; verilirse rol eşleşmesi de gerekir. */
  allowedRoles?: readonly UserRole[];
  children: ReactNode;
}

/**
 * Korumalı rota guard'ı: oturum yoksa login'e (geri dönüş adresi `from` ile taşınır),
 * rol uyuşmuyorsa 403 sayfasına yönlendirir. Bu yalnızca UX yönlendirmesidir — gerçek
 * yetki kontrolü her zaman backend'dedir ([Authorize] + kaynak sahipliği).
 */
export function ProtectedRoute({ allowedRoles, children }: ProtectedRouteProps) {
  const location = useLocation();
  const { session } = useAuth();

  if (session === null) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles !== undefined && !hasAnyRole(session, allowedRoles)) {
    return <Navigate to="/403" replace />;
  }

  return <>{children}</>;
}

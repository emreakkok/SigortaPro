import { Navigate } from "react-router-dom";
import { homePathFor, useAuth } from "@/features/auth/hooks/useAuth";

/** Kök adres ("/"): oturum yoksa login'e, varsa role göre portal/panele yönlendirir. */
export function RoleRedirect() {
  const { session } = useAuth();

  if (session === null) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={homePathFor(session)} replace />;
}

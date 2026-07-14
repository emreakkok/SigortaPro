import { useNavigate } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { Button } from "@/shared/components";

/**
 * Layout header'larına slot olarak enjekte edilen kullanıcı menüsü (ADR-029:
 * shared → features bağımlılığı oluşmaz; kompozisyon routes.tsx'te yapılır).
 */
export function UserMenu() {
  const { session, signOut } = useAuth();
  const navigate = useNavigate();

  if (session === null) {
    return null;
  }

  const handleSignOut = () => {
    signOut();
    navigate("/login", { replace: true });
  };

  return (
    <div className="flex items-center gap-3">
      <span className="hidden text-sm text-muted-foreground sm:inline">{session.email}</span>
      <Button variant="outline" size="sm" onClick={handleSignOut}>
        Çıkış
      </Button>
    </div>
  );
}

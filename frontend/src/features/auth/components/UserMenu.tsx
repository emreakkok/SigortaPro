import { useEffect, useRef, useState, type ReactNode } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { useMyProfile } from "@/features/profile/hooks/useProfile";
import {
  FileTextIcon,
  HomeIcon,
  LogoutIcon,
  ShieldIcon,
  UserIcon,
} from "@/shared/components";
import { hasAnyRole } from "@/shared/lib/session";
import { UserRoles } from "@/shared/types/auth.types";
import { cn } from "@/shared/lib/utils";

/**
 * Layout header'larına slot olarak enjekte edilen kullanıcı menüsü (ADR-029; kompozisyon routes.tsx'te).
 * ADR-040: avatar/ad tıklanınca modern bir dropdown açılır (el yazımı — ADR-027; harici menü kütüphanesi yok):
 * müşteri için Profil / Araçlarım / Konutlarım / Şifre Değiştir / Çıkış; personelde yalnızca Çıkış.
 * Ad-soyad müşteri profil sorgusundan gelir (ADR-039), yoksa e-postaya düşülür.
 */
export function UserMenu() {
  const { session, signOut } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const isCustomer = session !== null && hasAnyRole(session, [UserRoles.Customer]);
  const profile = useMyProfile(isCustomer);
  const fullName =
    isCustomer && profile.data !== undefined
      ? `${profile.data.firstName} ${profile.data.lastName}`.trim()
      : "";

  useEffect(() => {
    if (!open) {
      return;
    }
    function handleOutsideClick(event: MouseEvent) {
      if (containerRef.current !== null && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutsideClick);
    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("mousedown", handleOutsideClick);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  if (session === null) {
    return null;
  }

  const displayName = fullName.length > 0 ? fullName : session.email;
  const initials = getInitials(fullName.length > 0 ? fullName : session.email);

  const handleSignOut = () => {
    setOpen(false);
    signOut();
    navigate("/login", { replace: true });
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="flex items-center gap-2 rounded-full p-1 pr-2 transition-colors hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <span
          aria-hidden="true"
          className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/15 text-sm font-semibold text-primary"
        >
          {initials}
        </span>
        <span className="hidden max-w-[11rem] truncate text-sm font-medium sm:inline" title={session.email}>
          {displayName}
        </span>
        <span aria-hidden="true" className={cn("text-xs text-muted-foreground transition-transform", open && "rotate-180")}>
          ▾
        </span>
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-30 mt-2 w-56 overflow-hidden rounded-lg border bg-card py-1 shadow-lg"
        >
          <div className="border-b px-3 py-2">
            <p className="truncate text-sm font-medium">{displayName}</p>
            <p className="truncate text-xs text-muted-foreground">{session.email}</p>
          </div>

          {isCustomer && (
            <>
              <MenuLink to="/portal/profile" onSelect={() => setOpen(false)} icon={<UserIcon />}>
                Profil
              </MenuLink>
              <MenuLink to="/portal/profile?tab=vehicles" onSelect={() => setOpen(false)} icon={<FileTextIcon />}>
                Araçlarım
              </MenuLink>
              <MenuLink to="/portal/profile?tab=properties" onSelect={() => setOpen(false)} icon={<HomeIcon />}>
                Konutlarım
              </MenuLink>
              <MenuLink to="/portal/profile?tab=password" onSelect={() => setOpen(false)} icon={<ShieldIcon />}>
                Şifre Değiştir
              </MenuLink>
              <div className="my-1 border-t" />
            </>
          )}

          <button
            type="button"
            role="menuitem"
            onClick={handleSignOut}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-destructive transition-colors hover:bg-accent"
          >
            <LogoutIcon />
            Çıkış Yap
          </button>
        </div>
      )}
    </div>
  );
}

function MenuLink({
  to,
  icon,
  children,
  onSelect,
}: {
  to: string;
  icon: ReactNode;
  children: ReactNode;
  onSelect: () => void;
}) {
  return (
    <Link
      to={to}
      role="menuitem"
      onClick={onSelect}
      className="flex items-center gap-2 px-3 py-2 text-sm transition-colors hover:bg-accent"
    >
      {icon}
      {children}
    </Link>
  );
}

/** Ad-soyaddan (veya e-postadan) en fazla iki baş harf üretir (tr-TR büyük harf). */
function getInitials(source: string): string {
  const words = source.split(/[\s@._-]+/).filter((word) => word.length > 0);
  const letters = words.slice(0, 2).map((word) => word[0]);
  return (letters.join("") || "?").toLocaleUpperCase("tr-TR");
}

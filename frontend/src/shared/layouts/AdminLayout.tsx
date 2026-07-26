import type { ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useRoles } from "@/features/auth/hooks/useRoles";
import {
  AlertTriangleIcon,
  BellIcon,
  ChartIcon,
  FileTextIcon,
  ShieldCheckIcon,
  ShieldIcon,
  UserIcon,
  UsersIcon,
} from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { ThemeToggle } from "@/shared/theme/ThemeToggle";

/**
 * Admin paneli kabuğu: sol menü + üst çubuk + içerik alanı. Nav öğeleri ikon + etiketlidir (ADR-039).
 * `userMenu` slot'u routes.tsx'ten enjekte edilir — shared, features'a bağımlı olmaz (ADR-029).
 */
const NAV_ITEMS = [
  { to: "/admin", label: "Dashboard", end: true, icon: ChartIcon },
  { to: "/admin/customers", label: "Müşteriler", end: false, icon: UsersIcon },
  { to: "/admin/quotes", label: "Teklifler", end: false, icon: FileTextIcon },
  { to: "/admin/policies", label: "Poliçeler", end: false, icon: ShieldCheckIcon },
  { to: "/admin/claims", label: "Hasarlar", end: false, icon: AlertTriangleIcon },
  { to: "/admin/notifications", label: "Bildirimler", end: false, icon: BellIcon },
  // ADR-060: Personel yönetimi yalnızca Admin'e görünür (Personel bu menüyü görmez).
  { to: "/admin/staff", label: "Personel", end: false, icon: UserIcon, adminOnly: true },
  // ADR-048: Fiyatlandırma yönetimi ticari bir parametredir → yalnızca Admin görür (Personel'e gösterilmez).
  { to: "/admin/pricing", label: "Fiyatlandırma", end: false, icon: ChartIcon, adminOnly: true },
];

// `headerExtras` (ADR-041): userMenu'nün soluna eklenen ek header araçları (ör. bildirim zili) —
// userMenu gibi routes.tsx'ten slot olarak verilir (shared ↛ features, ADR-029).
export function AdminLayout({ userMenu, headerExtras }: { userMenu?: ReactNode; headerExtras?: ReactNode }) {
  // ADR-060: adminOnly menü öğeleri (Personel, Fiyatlandırma) yalnızca Admin'e gösterilir.
  // Rol bilgisi merkezi useRoles'tan gelir (oturum değişiminde yeniden render tetikler).
  const { isAdmin } = useRoles();
  const visibleNavItems = NAV_ITEMS.filter((item) => item.adminOnly !== true || isAdmin);

  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-60 shrink-0 flex-col border-r bg-card md:flex">
        <div className="flex h-16 items-center border-b px-6">
          <NavLink to="/admin" className="flex items-center gap-2 text-xl font-bold text-primary">
            <ShieldIcon className="h-5 w-5" />
            SigortaPro
          </NavLink>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                  isActive ? "bg-accent text-accent-foreground" : "text-muted-foreground",
                )
              }
            >
              <item.icon />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b bg-card/80 px-6 backdrop-blur supports-[backdrop-filter]:bg-card/70">
          <span className="font-semibold">Acente Yönetim Paneli</span>
          <div className="flex items-center gap-2">
            <ThemeToggle />
            {headerExtras}
            {userMenu}
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

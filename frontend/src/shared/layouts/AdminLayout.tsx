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
 * Admin paneli kabuğu: sol menü + üst çubuk + içerik alanı. Nav öğeleri ikon + etiketlidir.
 * `userMenu` slot'u routes.tsx'ten enjekte edilir — shared, features'a bağımlı olmaz.
 */
const NAV_ITEMS = [
  { to: "/admin", label: "Dashboard", end: true, icon: ChartIcon },
  { to: "/admin/customers", label: "Müşteriler", end: false, icon: UsersIcon },
  { to: "/admin/quotes", label: "Teklifler", end: false, icon: FileTextIcon },
  { to: "/admin/policies", label: "Poliçeler", end: false, icon: ShieldCheckIcon },
  { to: "/admin/claims", label: "Hasarlar", end: false, icon: AlertTriangleIcon },
  { to: "/admin/notifications", label: "Bildirimler", end: false, icon: BellIcon },
  // Personel yönetimi yalnızca Admin'e görünür (Personel bu menüyü görmez).
  { to: "/admin/staff", label: "Personel", end: false, icon: UserIcon, adminOnly: true },
  // Fiyatlandırma yönetimini acente personeli GÖRÜNTÜLER; değiştirme (taslak/aktifleştir) yalnızca
  // Admin'e açıktır (sayfa içinde ve backend'de kilitli) → menü tüm personele gösterilir.
  { to: "/admin/pricing", label: "Fiyatlandırma", end: false, icon: ChartIcon },
];

// `headerExtras`: userMenu'nün soluna eklenen ek header araçları (ör. bildirim zili) —
// userMenu gibi routes.tsx'ten slot olarak verilir (shared ↛ features).
export function AdminLayout({ userMenu, headerExtras }: { userMenu?: ReactNode; headerExtras?: ReactNode }) {
  // adminOnly menü öğeleri (Personel, Fiyatlandırma) yalnızca Admin'e gösterilir.
  // Rol bilgisi merkezi useRoles'tan gelir (oturum değişiminde yeniden render tetikler).
  const { isAdmin } = useRoles();
  const visibleNavItems = NAV_ITEMS.filter((item) => item.adminOnly !== true || isAdmin);

  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-64 shrink-0 flex-col border-r border-border/60 bg-card md:flex">
        {/* Logo: ikon rozeti + başlık/alt başlık — dengeli boşluk ve hizalama. */}
        <NavLink
          to="/admin"
          className="flex h-16 items-center gap-3 px-4 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <span
            aria-hidden="true"
            className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 text-primary"
          >
            <ShieldIcon className="h-5 w-5" />
          </span>
          <span className="flex min-w-0 flex-col leading-tight">
            <span className="truncate text-base font-bold tracking-tight text-foreground">SigortaPro</span>
            <span className="truncate text-xs text-muted-foreground">Yönetim Paneli</span>
          </span>
        </NavLink>

        <nav className="flex flex-1 flex-col gap-1 px-3 pb-4 pt-2">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "group relative flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                  isActive
                    ? "bg-primary/10 font-semibold text-primary"
                    : "font-medium text-muted-foreground hover:bg-accent/60 hover:text-foreground",
                )
              }
            >
              {({ isActive }) => (
                <>
                  {/* Aktif sayfa vurgusu: sol kenar aksan çubuğu + renkli arka plan. */}
                  <span
                    aria-hidden="true"
                    className={cn(
                      "absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-primary transition-opacity duration-200",
                      isActive ? "opacity-100" : "opacity-0",
                    )}
                  />
                  <item.icon className="h-5 w-5 shrink-0" />
                  <span className="truncate">{item.label}</span>
                </>
              )}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b border-border/60 bg-card/80 px-6 backdrop-blur supports-[backdrop-filter]:bg-card/70">
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

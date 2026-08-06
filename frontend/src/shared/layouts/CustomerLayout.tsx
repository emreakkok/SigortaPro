import type { ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import {
  AlertTriangleIcon,
  FileTextIcon,
  HomeIcon,
  RefreshIcon,
  ShieldCheckIcon,
  ShieldIcon,
} from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { ThemeToggle } from "@/shared/theme/ThemeToggle";

/**
 * Müşteri portalı kabuğu: üst navigasyon + içerik alanı. Tüm müşteri modülleri (teklif,
 * poliçe, hasar, yenileme, profil) –19 ile aktiftir. Nav öğeleri ikon + etiketlidir.
 * `userMenu` slot'u routes.tsx'ten enjekte edilir — shared, features'a bağımlı olmaz.
 */
// "Profilim" nav'dan kaldırıldı — profil/araçlar/konutlar/şifre artık sağ üstteki
// kullanıcı dropdown menüsünden erişilir.
const NAV_ITEMS = [
  { to: "/portal", label: "Ana Sayfa", end: true, icon: HomeIcon },
  { to: "/portal/quotes", label: "Tekliflerim", end: false, icon: FileTextIcon },
  { to: "/portal/policies", label: "Poliçelerim", end: false, icon: ShieldCheckIcon },
  { to: "/portal/claims", label: "Hasarlarım", end: false, icon: AlertTriangleIcon },
  { to: "/portal/renewals", label: "Yenilemeler", end: false, icon: RefreshIcon },
];

export function CustomerLayout({ userMenu }: { userMenu?: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-10 border-b bg-card/80 backdrop-blur supports-[backdrop-filter]:bg-card/70">
        <div className="container flex h-16 items-center justify-between">
          <div className="flex items-center gap-8">
            <NavLink to="/portal" className="flex items-center gap-2 text-xl font-bold text-primary">
              <ShieldIcon className="h-5 w-5" />
              SigortaPro
            </NavLink>
            <nav className="hidden items-center gap-1 md:flex">
              {NAV_ITEMS.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    cn(
                      "flex items-center gap-1.5 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                      isActive ? "bg-accent text-accent-foreground" : "text-muted-foreground",
                    )
                  }
                >
                  <item.icon />
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-1">
            <ThemeToggle />
            {userMenu}
          </div>
        </div>
      </header>
      <main className="container flex-1 py-8">
        <Outlet />
      </main>
      <footer className="border-t py-4">
        <div className="container text-sm text-muted-foreground">
          SigortaPro — MVP demo. Bu sistem gerçek bir sigorta ürünü satmaz.
        </div>
      </footer>
    </div>
  );
}

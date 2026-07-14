import type { ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { cn } from "@/shared/lib/utils";

/**
 * Müşteri portalı kabuğu: üst navigasyon + içerik alanı. Tüm müşteri modülleri (teklif,
 * poliçe, hasar, yenileme, profil) Task 16–19 ile aktiftir.
 * `userMenu` slot'u routes.tsx'ten enjekte edilir — shared, features'a bağımlı olmaz (ADR-029).
 */
const NAV_ITEMS = [
  { to: "/portal", label: "Ana Sayfa", end: true },
  { to: "/portal/quotes", label: "Tekliflerim", end: false },
  { to: "/portal/policies", label: "Poliçelerim", end: false },
  { to: "/portal/claims", label: "Hasarlarım", end: false },
  { to: "/portal/renewals", label: "Yenilemeler", end: false },
  { to: "/portal/profile", label: "Profilim", end: false },
];

export function CustomerLayout({ userMenu }: { userMenu?: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-10 border-b bg-card">
        <div className="container flex h-16 items-center justify-between">
          <div className="flex items-center gap-8">
            <NavLink to="/portal" className="text-xl font-bold text-primary">
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
                      "rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground",
                      isActive ? "bg-accent text-accent-foreground" : "text-muted-foreground",
                    )
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
          {userMenu}
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

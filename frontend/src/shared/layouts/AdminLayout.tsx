import type { ReactNode } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { cn } from "@/shared/lib/utils";

/**
 * Admin paneli kabuğu: sol menü + üst çubuk + içerik alanı.
 * `userMenu` slot'u routes.tsx'ten enjekte edilir — shared, features'a bağımlı olmaz (ADR-029).
 */
const NAV_ITEMS = [
  { to: "/admin", label: "Dashboard", end: true },
  { to: "/admin/customers", label: "Müşteriler" },
  { to: "/admin/quotes", label: "Teklifler" },
  { to: "/admin/policies", label: "Poliçeler" },
  { to: "/admin/claims", label: "Hasarlar" },
];

export function AdminLayout({ userMenu }: { userMenu?: ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-60 shrink-0 flex-col border-r bg-card md:flex">
        <div className="flex h-16 items-center border-b px-6">
          <NavLink to="/admin" className="text-xl font-bold text-primary">
            SigortaPro
          </NavLink>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3">
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
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b bg-card px-6">
          <span className="font-semibold">Acente Yönetim Paneli</span>
          {userMenu}
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { OperationalAlerts } from "@/features/dashboard/types/dashboard.types";
import {
  AlertTriangleIcon,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  FileTextIcon,
  RefreshIcon,
  ShieldCheckIcon,
} from "@/shared/components";
import { cn } from "@/shared/lib/utils";

interface ActionRow {
  key: string;
  icon: ReactNode;
  count: number;
  label: string;
  hint: string;
  /** Hedef admin ekranı; karşılığı olmayan satırlar bilgilendirmedir (tıklanamaz). */
  href: string | null;
  tone: "default" | "warning";
}

/**
 * Aksiyon Merkezi (ADR-052) — dashboard'ın en operasyonel bölümü: admin'in dokunması gereken açık işler.
 * Satırlar ilgili admin ekranına götürür; böylece dashboard yalnızca rapor değil, operasyonu yönlendiren
 * bir merkez olur. Sayaçlar birden fazla durumu kapsadığından (ör. "bekleyen" = Fiyatlandı + Onaylandı)
 * bağlantılar sayfayı **filtresiz** açar — sayaçla eşleşmeyen bir filtre uygulayıp yanıltmayız.
 * Ödeme için admin ekranı bulunmadığından o satır bilgilendirmedir (tıklanamaz).
 */
export function ActionCenter({
  alerts,
  showFinancials,
}: {
  alerts: OperationalAlerts;
  /** P1 D1: "Ödeme başarısız oldu" (tahsilat) FİNANSAL bir satırdır → yalnızca Admin'e gösterilir. */
  showFinancials: boolean;
}) {
  const rows: ActionRow[] = [
    {
      key: "quotes",
      icon: <FileTextIcon />,
      count: alerts.pendingQuotes,
      label: "Teklif işlem bekliyor",
      hint: "Fiyatlanmış veya onaylanmış, henüz satın alınmamış.",
      href: "/admin/quotes",
      tone: "default",
    },
    {
      key: "claims",
      icon: <AlertTriangleIcon />,
      count: alerts.pendingClaims,
      label: "Hasar değerlendirme bekliyor",
      hint: "Bildirilmiş veya incelemede.",
      href: "/admin/claims",
      tone: "warning",
    },
    {
      key: "renewals",
      icon: <RefreshIcon />,
      count: alerts.upcomingRenewals,
      label: "Poliçe yenilemesi yaklaşıyor",
      hint: `Önümüzdeki ${alerts.upcomingRenewalWindowDays} gün içinde bitecek aktif poliçeler.`,
      href: "/admin/policies",
      tone: "default",
    },
  ];

  // Tahsilat/başarısız ödeme satırı yalnızca Admin'e eklenir (finansal — Personel'de backend'de null'dır).
  if (showFinancials) {
    rows.push({
      key: "payments",
      icon: <ShieldCheckIcon />,
      count: alerts.failedPayments ?? 0,
      label: "Ödeme başarısız oldu",
      hint: "Seçilen dönemde başarısız ödeme denemesi (tahsilat kaybı).",
      href: null,
      tone: "warning",
    });
  }

  const actionable = rows.filter((row) => row.count > 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Aksiyon Merkezi</CardTitle>
        <CardDescription>Dikkat isteyen açık işler — satıra tıklayarak ilgili ekrana gidin.</CardDescription>
      </CardHeader>
      <CardContent>
        {actionable.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Bekleyen iş yok — tüm operasyon güncel. 🎉
          </p>
        ) : (
          <ul className="space-y-2">
            {actionable.map((row) => {
              const content = (
                <div
                  className={cn(
                    "flex items-center gap-3 rounded-lg border p-3 transition-colors",
                    row.href !== null && "hover:border-primary/40 hover:bg-accent/50",
                  )}
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "flex h-9 w-9 shrink-0 items-center justify-center rounded-lg [&_svg]:h-[1.1rem] [&_svg]:w-[1.1rem]",
                      row.tone === "warning"
                        ? "bg-warning/10 text-warning"
                        : "bg-accent text-accent-foreground",
                    )}
                  >
                    {row.icon}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium">
                      <span className="tabular-nums">{row.count}</span> {row.label}
                    </p>
                    <p className="truncate text-xs text-muted-foreground">{row.hint}</p>
                  </div>
                  {row.href !== null && (
                    <span aria-hidden="true" className="shrink-0 text-muted-foreground">
                      →
                    </span>
                  )}
                </div>
              );

              return (
                <li key={row.key}>
                  {row.href === null ? content : <Link to={row.href}>{content}</Link>}
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

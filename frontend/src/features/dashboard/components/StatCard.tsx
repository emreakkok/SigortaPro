import type { ReactNode } from "react";
import { Card } from "@/shared/components";

interface StatCardProps {
  title: string;
  value: ReactNode;
  hint?: string;
  /** Sağ üstte yumuşak rozet içinde gösterilen ikon (tasarım-sistemi ikonu). */
  icon?: ReactNode;
  /** Değerin altına eklenen içerik — ör. gerçek veriyle beslenen sparkline. */
  footer?: ReactNode;
}

/*
 * Dashboard KPI kartı (ADR-044). Sadece sayı yerine kurumsal SaaS düzeni: başlık + ikon rozeti +
 * büyük tabular değer + opsiyonel açıklama ve alt görsel (sparkline). Hover'da hafif yükselir
 * (Card taban geçişini kullanır). Renkler token tabanlı → Dark Mode uyumlu.
 */
export function StatCard({ title, value, hint, icon, footer }: StatCardProps) {
  return (
    <Card className="p-5 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        {icon !== undefined && (
          <span
            aria-hidden="true"
            className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-accent text-accent-foreground [&_svg]:h-[1.1rem] [&_svg]:w-[1.1rem]"
          >
            {icon}
          </span>
        )}
      </div>
      <p className="mt-2 text-3xl font-bold tracking-tight tabular-nums">{value}</p>
      {hint !== undefined && <p className="mt-1 text-xs text-muted-foreground">{hint}</p>}
      {footer !== undefined && <div className="mt-3">{footer}</div>}
    </Card>
  );
}

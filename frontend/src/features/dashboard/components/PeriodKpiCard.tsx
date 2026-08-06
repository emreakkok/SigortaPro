import type { ReactNode } from "react";
import { Card } from "@/shared/components";
import { cn } from "@/shared/lib/utils";

interface PeriodKpiCardProps {
  title: string;
  value: ReactNode;
  icon?: ReactNode;
  /**
   * Önceki eşit uzunluktaki döneme göre oransal değişim (0.18 = +%18).
   * **null** → karşılaştırma yapılamıyor (önceki dönem 0). Bu durumda oran YERİNE nötr bir açıklama gösterilir;
   * "+%100" gibi yanıltıcı bir artış asla uydurulmaz.
   */
  delta: number | null;
  /** Karşılaştırma etiketinin sonu — ör. "geçen haftaya göre". */
  comparisonLabel: string;
  /** Artışın iyi mi kötü mü olduğu. Hasar gibi metriklerde artış olumsuzdur. */
  higherIsBetter?: boolean;
}

function formatDelta(delta: number): string {
  const sign = delta > 0 ? "+" : "";
  return `${sign}${(delta * 100).toLocaleString("tr-TR", { maximumFractionDigits: 1 })}%`;
}

/**
 * Dönemsel KPI kartı: büyük değer + önceki eş dönemle karşılaştırma.
 * Karşılaştırma güvenilir değilse (önceki dönem 0) oran gösterilmez — dashboard yanıltıcı sayı üretmez.
 */
export function PeriodKpiCard({
  title,
  value,
  icon,
  delta,
  comparisonLabel,
  higherIsBetter = true,
}: PeriodKpiCardProps) {
  const flat = delta !== null && Math.abs(delta) < 0.0005;
  const positive = delta !== null && delta > 0;
  const good = positive === higherIsBetter;

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

      <div className="mt-2 flex flex-wrap items-center gap-1.5 text-xs">
        {delta === null ? (
          <span className="text-muted-foreground">Önceki dönemde veri yok</span>
        ) : (
          <>
            <span
              className={cn(
                "inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 font-medium tabular-nums",
                flat
                  ? "bg-muted text-muted-foreground"
                  : good
                    ? "bg-success/10 text-success"
                    : "bg-warning/10 text-warning",
              )}
            >
              {!flat && <span aria-hidden="true">{positive ? "▲" : "▼"}</span>}
              {formatDelta(delta)}
            </span>
            <span className="text-muted-foreground">{comparisonLabel}</span>
          </>
        )}
      </div>
    </Card>
  );
}

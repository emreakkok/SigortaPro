import { cn } from "@/shared/lib/utils";

/** Dashboard hazır tarih aralıkları. Backend aralığı kapsayıcı (inclusive) yorumlar. */
export const DASHBOARD_RANGES = ["today", "week", "month", "last30"] as const;
export type DashboardRangeKey = (typeof DASHBOARD_RANGES)[number];

const RANGE_LABELS: Record<DashboardRangeKey, string> = {
  today: "Bugün",
  week: "Bu Hafta",
  month: "Bu Ay",
  last30: "Son 30 Gün",
};

/** Yerel gün başlangıcı (00:00) — "bugün"/"bu hafta" sınırları kullanıcının takvimine göre olmalı. */
function startOfDay(date: Date): Date {
  const copy = new Date(date);
  copy.setHours(0, 0, 0, 0);
  return copy;
}

/** Haftanın başlangıcı: Pazartesi (TR takvimi). */
function startOfWeek(date: Date): Date {
  const start = startOfDay(date);
  const weekday = (start.getDay() + 6) % 7; // Pazartesi = 0
  start.setDate(start.getDate() - weekday);
  return start;
}

/**
 * Seçili aralığı mutlak `from`/`to` ISO değerlerine çevirir. `to` her zaman "şu an"dır — böylece
 * dönem içi kısmi gün de kapsanır ve backend'in "önceki eşit uzunluktaki dönem" hesabı doğru normalize olur.
 */
export function resolveRange(key: DashboardRangeKey, now: Date = new Date()): { from: string; to: string } {
  const to = now;
  const from =
    key === "today"
      ? startOfDay(now)
      : key === "week"
        ? startOfWeek(now)
        : key === "month"
          ? new Date(now.getFullYear(), now.getMonth(), 1)
          : new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);

  return { from: from.toISOString(), to: to.toISOString() };
}

/**
 * Tarih aralığı seçimi — mevcut `PageSizeSelector` segmented control diliyle.
 * Tek bir seçim TÜM dashboard bloklarını tutarlı biçimde etkiler (tek sorgu yenilenir).
 */
export function DashboardRangeFilter({
  value,
  onChange,
}: {
  value: DashboardRangeKey;
  onChange: (key: DashboardRangeKey) => void;
}) {
  return (
    <div
      role="group"
      aria-label="Tarih aralığı"
      className="inline-flex items-center rounded-lg border bg-muted p-0.5"
    >
      {DASHBOARD_RANGES.map((key) => {
        const active = key === value;
        return (
          <button
            key={key}
            type="button"
            aria-pressed={active}
            onClick={() => onChange(key)}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              active
                ? "bg-primary text-primary-foreground shadow-sm"
                : "text-muted-foreground hover:bg-background hover:text-foreground",
            )}
          >
            {RANGE_LABELS[key]}
          </button>
        );
      })}
    </div>
  );
}

export { RANGE_LABELS as DASHBOARD_RANGE_LABELS };

import { cn } from "@/shared/lib/utils";
import { ADMIN_PAGE_SIZE_OPTIONS, type AdminPageSize } from "@/shared/lib/pagination";

interface PageSizeSelectorProps {
  value: AdminPageSize;
  onChange: (size: AdminPageSize) => void;
}

/*
 * Admin sayfa boyutu seçimi (ADR-045). Klasik <select> yerine modern **segmented control**:
 * bir kapsayıcı içinde chip'ler; aktif seçim `bg-primary` ile belirgin. Renkler token tabanlı
 * (`bg-muted`/`bg-primary`/`bg-background`) → Dark Mode uyumlu.
 */
export function PageSizeSelector({ value, onChange }: PageSizeSelectorProps) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-sm text-muted-foreground">Sayfa başına</span>
      <div
        role="group"
        aria-label="Sayfa başına kayıt sayısı"
        className="inline-flex items-center rounded-lg border bg-muted p-0.5"
      >
        {ADMIN_PAGE_SIZE_OPTIONS.map((option) => {
          const active = option === value;
          return (
            <button
              key={option}
              type="button"
              aria-pressed={active}
              onClick={() => onChange(option)}
              className={cn(
                "min-w-9 rounded-md px-2.5 py-1 text-sm font-medium tabular-nums transition-colors",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                active
                  ? "bg-primary text-primary-foreground shadow-sm"
                  : "text-muted-foreground hover:bg-background hover:text-foreground",
              )}
            >
              {option}
            </button>
          );
        })}
      </div>
    </div>
  );
}

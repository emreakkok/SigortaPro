import type { PricingBreakdownItem } from "@/features/quotes/types/quote.types";
import { cn } from "@/shared/lib/utils";
import { formatMultiplier, formatMultiplierEffect } from "@/shared/utils/format";

/**
 * Prim dökümü: her risk faktörünün prime etkisi (fiyatlama şeffaflığı). Çıplak "×1,25" değeri
 * kullanıcı için anlamlı olmadığından etki, "+%25 ek prim" / "−%10 indirim" diline çevrilir ve
 * artış/indirim renkle ayrıştırılır (ADR-039 — yalnızca sunum; hesaplama backend'dedir).
 */
export function PremiumBreakdownList({ items }: { items: PricingBreakdownItem[] }) {
  return (
    <ul className="divide-y divide-border text-sm">
      {items.map((item) => {
        const delta = item.multiplier - 1;
        const isNeutral = Math.abs(delta) < 0.0001;
        return (
          <li key={item.factor} className="flex items-start justify-between gap-4 py-2">
            <div>
              <p className="font-medium">{item.factor}</p>
              <p className="text-muted-foreground">{item.description}</p>
            </div>
            <div className="shrink-0 text-right">
              <p
                className={cn(
                  "font-medium",
                  isNeutral && "text-muted-foreground",
                  !isNeutral && delta > 0 && "text-destructive",
                  !isNeutral && delta < 0 && "text-success",
                )}
              >
                {formatMultiplierEffect(item.multiplier)}
              </p>
              <p className="font-mono text-xs text-muted-foreground">
                {formatMultiplier(item.multiplier)}
              </p>
            </div>
          </li>
        );
      })}
    </ul>
  );
}

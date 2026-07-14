import type { PricingBreakdownItem } from "@/features/quotes/types/quote.types";
import { formatMultiplier } from "@/shared/utils/format";

/** Prim dökümü: her risk faktörünün çarpanı ve açıklaması (fiyatlama şeffaflığı). */
export function PremiumBreakdownList({ items }: { items: PricingBreakdownItem[] }) {
  return (
    <ul className="divide-y divide-border text-sm">
      {items.map((item) => (
        <li key={item.factor} className="flex items-start justify-between gap-4 py-2">
          <div>
            <p className="font-medium">{item.factor}</p>
            <p className="text-muted-foreground">{item.description}</p>
          </div>
          <span className="shrink-0 font-mono text-muted-foreground">
            {formatMultiplier(item.multiplier)}
          </span>
        </li>
      ))}
    </ul>
  );
}

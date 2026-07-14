import type { QuoteCoverage } from "@/features/quotes/types/quote.types";
import { formatCurrency } from "@/shared/utils/format";

/** Teminat kalemleri ve (paket seviyesine göre ölçeklenmiş) limitleri. */
export function CoverageList({ coverages }: { coverages: QuoteCoverage[] }) {
  return (
    <ul className="divide-y divide-border text-sm">
      {coverages.map((coverage) => (
        <li key={coverage.name} className="flex items-start justify-between gap-4 py-2">
          <div>
            <p className="font-medium">{coverage.name}</p>
            {coverage.description !== null && (
              <p className="text-muted-foreground">{coverage.description}</p>
            )}
          </div>
          <span className="shrink-0 font-medium">{formatCurrency(coverage.limit)}</span>
        </li>
      ))}
    </ul>
  );
}

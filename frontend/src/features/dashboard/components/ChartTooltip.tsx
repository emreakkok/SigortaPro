import type { TooltipProps } from "recharts";
import { formatCurrency } from "@/shared/utils/format";

interface ChartDatum {
  label: string;
  premiumTotal: number;
  policyCount: number;
}

/**
 * İki dashboard grafiğinin ortak hover tooltip'i: etiket + prim toplamı + poliçe adedi.
 * Tema token'larıyla stillenir; metin serinin rengini değil metin renklerini kullanır.
 */
export function ChartTooltip({ active, payload }: TooltipProps<number, string>) {
  if (active !== true || payload === undefined || payload.length === 0) {
    return null;
  }

  const datum = payload[0].payload as ChartDatum;

  return (
    <div className="rounded-md border bg-card px-3 py-2 text-sm shadow-sm">
      <p className="font-medium">{datum.label}</p>
      <p className="mt-1 text-muted-foreground">
        Prim: <span className="font-medium text-foreground">{formatCurrency(datum.premiumTotal)}</span>
      </p>
      <p className="text-muted-foreground">
        Poliçe: <span className="font-medium text-foreground">{datum.policyCount}</span>
      </p>
    </div>
  );
}

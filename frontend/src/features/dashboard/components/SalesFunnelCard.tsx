import type { QuoteFunnel } from "@/features/dashboard/types/dashboard.types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/components";
import { formatPercent } from "@/shared/utils/format";

/**
 * Satış hunisi. Kohort: seçilen dönemde OLUŞTURULAN teklifler ve bunların bugünkü durumu.
 * "Onaylanan" adımı satın alınanları da içerir (satın alma onaydan geçer) → huni monoton azalır.
 * Dönüşüm oranı gerçek `Quote.Status` yaşam döngüsünden hesaplanır; teklif yoksa oran **gösterilmez**
 * (tanımsızdır — "%0" yanıltıcı olurdu).
 */
export function SalesFunnelCard({ funnel }: { funnel: QuoteFunnel }) {
  const steps = [
    { label: "Oluşturulan teklif", value: funnel.created },
    { label: "Onaylanan", value: funnel.approved },
    { label: "Satın alınan (poliçeleşen)", value: funnel.purchased },
  ];

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <CardTitle>Satış Hunisi</CardTitle>
            <CardDescription>Bu dönemde oluşturulan tekliflerin bugünkü durumu.</CardDescription>
          </div>
          {funnel.conversionRate !== null && (
            <div className="text-right">
              <p className="text-2xl font-bold tabular-nums text-primary">
                {formatPercent(funnel.conversionRate)}
              </p>
              <p className="text-xs text-muted-foreground">Teklif → Poliçe</p>
            </div>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {funnel.created === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Seçilen dönemde teklif oluşturulmamış.
          </p>
        ) : (
          <div className="space-y-3">
            {steps.map((step) => {
              const ratio = funnel.created === 0 ? 0 : step.value / funnel.created;
              return (
                <div key={step.label}>
                  <div className="flex items-baseline justify-between gap-2 text-sm">
                    <span className="text-muted-foreground">{step.label}</span>
                    <span className="font-semibold tabular-nums">{step.value}</span>
                  </div>
                  <div className="mt-1.5 h-2.5 overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-primary transition-[width] duration-500"
                      style={{ width: `${Math.max(ratio * 100, step.value > 0 ? 2 : 0)}%` }}
                    />
                  </div>
                </div>
              );
            })}

            <div className="flex flex-wrap gap-x-6 gap-y-1 border-t pt-3 text-xs text-muted-foreground">
              <span>
                Süresi dolan: <span className="font-medium tabular-nums text-foreground">{funnel.expired}</span>
              </span>
              <span>
                Reddedilen: <span className="font-medium tabular-nums text-foreground">{funnel.rejected}</span>
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

import type { BranchPerformance } from "@/features/dashboard/types/dashboard.types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/components";
import { INSURANCE_BRANCH_LABELS } from "@/shared/types/insurance.types";
import { formatCurrency, formatPercent } from "@/shared/utils/format";

/**
 * Branş performansı (ADR-052). Tek kohort/tek sorgu: dönemde oluşturulan teklifler, bunların poliçeleşen
 * kısmı ve primi. Aynı kaynaktan geldiği için dönüşüm oranı asla %100'ü aşamaz (dönem kayması yok).
 * Prim çubuğu, en yüksek üreten branşa göre normalize edilir → hangi branşın işletmeye daha çok katkı
 * sağladığı ilk bakışta okunur. Teklifi olmayan branşta oran gösterilmez (tanımsız).
 */
export function BranchPerformanceCard({
  data,
  showFinancials,
}: {
  data: BranchPerformance[];
  /** P1 D1: "Prim" sütunu ve prim çubuğu FİNANSAL → yalnızca Admin. Teklif/Poliçe/Dönüşüm operasyoneldir. */
  showFinancials: boolean;
}) {
  const maxPremium = showFinancials
    ? data.reduce((max, row) => Math.max(max, row.premiumTotal), 0)
    : 0;
  const hasActivity = data.some((row) => row.quoteCount > 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Branş Performansı</CardTitle>
        <CardDescription>
          Bu dönemde oluşturulan teklifler ve poliçeleşen kısmı — prim, poliçeleşen tekliflerindir.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {!hasActivity ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Seçilen dönemde branş hareketi yok.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="py-2 pr-4 font-medium">Branş</th>
                  <th className="py-2 pr-4 text-right font-medium">Teklif</th>
                  <th className="py-2 pr-4 text-right font-medium">Poliçe</th>
                  <th className="py-2 pr-4 text-right font-medium">Dönüşüm</th>
                  {showFinancials && <th className="py-2 text-right font-medium">Prim</th>}
                </tr>
              </thead>
              <tbody>
                {data.map((row) => (
                  <tr key={row.branch} className="border-b last:border-0">
                    <td className="py-2.5 pr-4">
                      <div className="font-medium">{INSURANCE_BRANCH_LABELS[row.branch]}</div>
                      {showFinancials && (
                        <div className="mt-1 h-1.5 w-full max-w-[7rem] overflow-hidden rounded-full bg-muted">
                          <div
                            className="h-full rounded-full bg-primary"
                            style={{
                              width: maxPremium === 0 ? "0%" : `${(row.premiumTotal / maxPremium) * 100}%`,
                            }}
                          />
                        </div>
                      )}
                    </td>
                    <td className="py-2.5 pr-4 text-right tabular-nums">{row.quoteCount}</td>
                    <td className="py-2.5 pr-4 text-right tabular-nums">{row.purchasedCount}</td>
                    <td className="py-2.5 pr-4 text-right tabular-nums">
                      {row.conversionRate === null ? (
                        <span className="text-muted-foreground">—</span>
                      ) : (
                        formatPercent(row.conversionRate)
                      )}
                    </td>
                    {showFinancials && (
                      <td className="py-2.5 text-right font-semibold tabular-nums">
                        {formatCurrency(row.premiumTotal)}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

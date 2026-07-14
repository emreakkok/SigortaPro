import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/components";
import { useRiskiestCustomers } from "@/features/dashboard/hooks/useDashboard";
import { formatCurrency } from "@/shared/utils/format";

const TOP_SEGMENT_COUNT = 5;

/** En riskli müşteri segmentleri: fiyatlamaya etki eden hasar sayısına göre ilk 5 müşteri. */
export function RiskiestCustomersCard() {
  const { data, isLoading, isError } = useRiskiestCustomers(TOP_SEGMENT_COUNT);

  return (
    <Card>
      <CardHeader>
        <CardTitle>En Riskli Müşteriler</CardTitle>
        <CardDescription>Onaylanan/ödenen hasar sayısına göre ilk {TOP_SEGMENT_COUNT}.</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <p className="py-4 text-sm text-muted-foreground">Yükleniyor…</p>
        ) : isError || data === undefined ? (
          <p className="py-4 text-sm text-destructive">Segment verisi alınamadı.</p>
        ) : data.length === 0 ? (
          <p className="py-4 text-sm text-muted-foreground">Hasarlı müşteri kaydı yok.</p>
        ) : (
          <ul className="divide-y divide-border text-sm">
            {data.map((segment) => (
              <li key={segment.customerId} className="flex items-center justify-between gap-4 py-2">
                <div className="min-w-0">
                  <p className="truncate font-medium">{segment.fullName}</p>
                  <p className="text-muted-foreground">{segment.claimCount} hasar</p>
                </div>
                <span className="shrink-0 font-medium tabular-nums">
                  {formatCurrency(segment.totalClaimAmount)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

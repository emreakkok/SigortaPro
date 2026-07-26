import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  Skeleton,
  UsersIcon,
} from "@/shared/components";
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
          <div className="space-y-3 py-1" aria-hidden="true">
            {Array.from({ length: TOP_SEGMENT_COUNT }).map((_, index) => (
              <div key={index} className="flex items-center justify-between gap-4">
                <div className="min-w-0 flex-1 space-y-1.5">
                  <Skeleton className="h-3.5 w-2/5" />
                  <Skeleton className="h-3 w-1/4" />
                </div>
                <Skeleton className="h-4 w-16" />
              </div>
            ))}
          </div>
        ) : isError || data === undefined ? (
          <p className="py-4 text-sm text-destructive">Segment verisi alınamadı.</p>
        ) : data.length === 0 ? (
          <EmptyState
            className="py-8"
            icon={<UsersIcon />}
            title="Hasarlı müşteri yok"
            description="Onaylanan veya ödenen hasarı olan müşteri bulunmuyor."
          />
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

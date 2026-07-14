import { BranchDistributionChart } from "@/features/dashboard/components/BranchDistributionChart";
import { MonthlySalesChart } from "@/features/dashboard/components/MonthlySalesChart";
import { RiskiestCustomersCard } from "@/features/dashboard/components/RiskiestCustomersCard";
import { StatCard } from "@/features/dashboard/components/StatCard";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboard";
import {
  Alert,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { formatCurrency, formatPercent } from "@/shared/utils/format";

/**
 * Acente dashboard'u: KPI kartları (prim üretimi, aktif poliçe, bekleyenler, oranlar)
 * + aylık prim trendi ve branş dağılımı grafikleri + en riskli müşteri segmentleri.
 * Tüm veriler Task 14 dashboard uçlarından okunur (salt okunur).
 */
export default function AdminDashboardPage() {
  const { data, isLoading, isError, error } = useDashboardSummary();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Acentenin üretim, poliçe ve hasar görünümü.</p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <StatCard title="Toplam Prim Üretimi" value={formatCurrency(data.totalPremiumProduction)} />
            <StatCard title="Aktif Poliçe" value={data.activePolicyCount} />
            <StatCard
              title="Bekleyen Teklif"
              value={data.pendingQuoteCount}
              hint="Fiyatlanmış veya onaylanmış, henüz satın alınmamış."
            />
            <StatCard
              title="Bekleyen Hasar"
              value={data.pendingClaimCount}
              hint="Bildirilmiş veya incelemede."
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <StatCard
              title="Yenileme Oranı"
              value={formatPercent(data.renewalRate)}
              hint="Onaylanan yenileme / sunulan yenileme."
            />
            <StatCard
              title="Hasar/Prim Oranı"
              value={formatPercent(data.claimToPremiumRatio)}
              hint="Ödenen hasar tutarı / üretilen prim."
            />
          </div>

          <div className="grid gap-4 xl:grid-cols-3">
            <Card className="xl:col-span-2">
              <CardHeader>
                <CardTitle>Aylık Prim Üretimi</CardTitle>
                <CardDescription>Son 12 ay — poliçe oluşturulma ayına göre.</CardDescription>
              </CardHeader>
              <CardContent>
                <MonthlySalesChart data={data.monthlySales} />
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Branş Dağılımı</CardTitle>
                <CardDescription>Teklif branşına göre prim toplamı.</CardDescription>
              </CardHeader>
              <CardContent>
                <BranchDistributionChart data={data.branchDistribution} />
              </CardContent>
            </Card>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <RiskiestCustomersCard />
          </div>
        </>
      )}
    </div>
  );
}

import { useMemo, useState } from "react";
import { ActionCenter } from "@/features/dashboard/components/ActionCenter";
import { ActivityFeedCard } from "@/features/dashboard/components/ActivityFeedCard";
import { BranchPerformanceCard } from "@/features/dashboard/components/BranchPerformanceCard";
import { ClaimOperationCard } from "@/features/dashboard/components/ClaimOperationCard";
import {
  DASHBOARD_RANGE_LABELS,
  DashboardRangeFilter,
  resolveRange,
  type DashboardRangeKey,
} from "@/features/dashboard/components/DashboardRangeFilter";
import { PeriodKpiCard } from "@/features/dashboard/components/PeriodKpiCard";
import { PremiumSeriesChart } from "@/features/dashboard/components/PremiumSeriesChart";
import { RiskiestCustomersCard } from "@/features/dashboard/components/RiskiestCustomersCard";
import { SalesFunnelCard } from "@/features/dashboard/components/SalesFunnelCard";
import { StatCard } from "@/features/dashboard/components/StatCard";
import { useDashboardSummary } from "@/features/dashboard/hooks/useDashboard";
import { useRoles } from "@/features/auth/hooks/useRoles";
import {
  Alert,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ChartIcon,
  FileTextIcon,
  RefreshIcon,
  ShieldCheckIcon,
  ShieldIcon,
  Skeleton,
  UsersIcon,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { formatCurrency, formatPercent } from "@/shared/utils/format";

/** Karşılaştırma etiketi: seçilen aralığın "önceki eş dönemi" nasıl adlandırılır. */
const COMPARISON_LABELS: Record<DashboardRangeKey, string> = {
  today: "düne göre",
  week: "önceki haftaya göre",
  month: "önceki aya göre",
  last30: "önceki 30 güne göre",
};

/**
 * Acente operasyon dashboard'u (ADR-052). Görsel hiyerarşi 4 soruya göre kurulur:
 * (A) bu dönem ne oldu → KPI şeridi, (C) nerede problem var → Aksiyon Merkezi,
 * (B) nasıl gidiyoruz → prim serisi + huni + branş, (D) fırsat/portföy → hasar, aktivite, riskli müşteriler.
 * Tüm bloklar TEK sorgudan beslenir; tarih aralığı değiştiğinde hepsi tutarlı biçimde yenilenir.
 * Hiçbir sayı frontend'de hesaplanmaz — tümü backend aggregate'lerinden gelir.
 */
export default function AdminDashboardPage() {
  const [range, setRange] = useState<DashboardRangeKey>("month");
  // P1 kararı D1/D4: agregat finansal kartlar yalnızca Admin'e render edilir. Backend zaten Personel için
  // finansal alanları null'lar (P2); burada rol bazlı olarak hiç render etmeyiz (null kontrolü değil).
  const { isAdmin } = useRoles();

  // Aralık yalnızca seçim değiştiğinde hesaplanır → query key sabit kalır, gereksiz istek olmaz.
  const params = useMemo(() => resolveRange(range), [range]);
  const { data, isLoading, isError, error, isFetching } = useDashboardSummary(params);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            {DASHBOARD_RANGE_LABELS[range]} — üretim, satış hattı ve operasyon görünümü.
          </p>
        </div>
        <DashboardRangeFilter value={range} onChange={setRange} />
      </div>

      {isLoading ? (
        <DashboardSkeleton />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : (
        <div className={isFetching ? "space-y-6 opacity-60 transition-opacity" : "space-y-6"}>
          {/* A — Bu dönem ne oldu? Önceki eş dönemle karşılaştırmalı. */}
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {/* Prim Üretimi FİNANSAL → yalnızca Admin (D1). */}
            {isAdmin && (
              <PeriodKpiCard
                title="Prim Üretimi"
                value={formatCurrency(data.current.premiumProduction)}
                icon={<ChartIcon />}
                delta={data.deltas.premiumProduction}
                comparisonLabel={COMPARISON_LABELS[range]}
              />
            )}
            <PeriodKpiCard
              title="Üretilen Poliçe"
              value={data.current.newPolicies}
              icon={<ShieldCheckIcon />}
              delta={data.deltas.newPolicies}
              comparisonLabel={COMPARISON_LABELS[range]}
            />
            <PeriodKpiCard
              title="Yeni Teklif"
              value={data.current.newQuotes}
              icon={<FileTextIcon />}
              delta={data.deltas.newQuotes}
              comparisonLabel={COMPARISON_LABELS[range]}
            />
            <PeriodKpiCard
              title="Yeni Müşteri"
              value={data.current.newCustomers}
              icon={<UsersIcon />}
              delta={data.deltas.newCustomers}
              comparisonLabel={COMPARISON_LABELS[range]}
            />
          </div>

          {/* C — Nerede problem var? En üstte, çünkü aksiyon gerektirir. */}
          <div className="grid gap-4 xl:grid-cols-3">
            {/* Personel'de prim serisi gizlendiğinden Aksiyon Merkezi tam genişliğe alınır. */}
            <div className={isAdmin ? "xl:col-span-1" : "xl:col-span-3"}>
              <ActionCenter alerts={data.alerts} showFinancials={isAdmin} />
            </div>

            {/* B — Nasıl gidiyoruz? Prim üretimi zaman serisi (FİNANSAL → yalnızca Admin, D1). */}
            {isAdmin && (
              <Card className="xl:col-span-2">
                <CardHeader>
                  <CardTitle>Prim Üretimi</CardTitle>
                  <CardDescription>
                    {DASHBOARD_RANGE_LABELS[range]} — brüt üretilen prim. Poliçenin{" "}
                    <strong>üretim tarihi</strong> baz alınmıştır (poliçe listesindeki teminat başlangıcından
                    farklı olabilir).
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <PremiumSeriesChart data={data.premiumSeries} granularity={data.granularity} />
                </CardContent>
              </Card>
            )}
          </div>

          {/* D — Satış hattı ve branş performansı. Branş prim tutarları FİNANSAL → yalnızca Admin (D1). */}
          <div className="grid gap-4 xl:grid-cols-2">
            <SalesFunnelCard funnel={data.funnel} />
            <BranchPerformanceCard data={data.branchPerformance} showFinancials={isAdmin} />
          </div>

          {/* Hasar operasyonu + son aktiviteler. Hasar tutar toplamları FİNANSAL → yalnızca Admin (D1). */}
          <div className="grid gap-4 xl:grid-cols-2">
            <ClaimOperationCard claims={data.claims} showFinancials={isAdmin} />
            <ActivityFeedCard />
          </div>

          {/* Portföy (dönemden bağımsız anlık durum) + riskli müşteriler. */}
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <StatCard
              title="Aktif Poliçe"
              value={data.portfolio.activePolicyCount}
              icon={<ShieldCheckIcon />}
              hint="Portföydeki yürürlükteki poliçeler."
            />
            <StatCard
              title="Toplam Müşteri"
              value={data.portfolio.totalCustomerCount}
              icon={<UsersIcon />}
              hint="Kayıtlı müşteri sayısı."
            />
            <StatCard
              title="Yenileme Oranı"
              value={data.renewalRate === null ? "—" : formatPercent(data.renewalRate)}
              icon={<RefreshIcon />}
              hint={
                data.renewalRate === null
                  ? "Bu dönemde yenileme sunulmadı."
                  : "Bu dönemde sunulan yenilemelerin kabul oranı."
              }
            />
            {/* Hasar/Prim Oranı (kârlılık) FİNANSAL → yalnızca Admin (D1). */}
            {isAdmin && (
              <StatCard
                title="Hasar/Prim Oranı"
                value={data.portfolio.lossRatio === null ? "—" : formatPercent(data.portfolio.lossRatio)}
                icon={<ShieldIcon />}
                hint="Kümülatif: ödenen hasar / üretilen prim."
              />
            )}
          </div>

          {/* Riskli müşteriler (hasar tutarı + profilleme) yalnızca Admin (D3 — endpoint de Admin-only). */}
          {isAdmin && (
            <div className="grid gap-4 md:grid-cols-2">
              <RiskiestCustomersCard />
            </div>
          )}
        </div>
      )}
    </div>
  );
}

/** Yükleme iskeleti: KPI şeridi + aksiyon/grafik alanı (layout zıplaması olmadan). */
function DashboardSkeleton() {
  return (
    <div className="space-y-6" aria-hidden="true">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <Card key={index} className="p-5">
            <div className="flex items-start justify-between">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-9 w-9 rounded-lg" />
            </div>
            <Skeleton className="mt-3 h-8 w-28" />
            <Skeleton className="mt-3 h-4 w-32" />
          </Card>
        ))}
      </div>
      <div className="grid gap-4 xl:grid-cols-3">
        <Card className="p-6">
          <Skeleton className="h-5 w-36" />
          <div className="mt-4 space-y-2">
            {Array.from({ length: 3 }).map((_, index) => (
              <Skeleton key={index} className="h-14 w-full rounded-lg" />
            ))}
          </div>
        </Card>
        <Card className="p-6 xl:col-span-2">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="mt-4 h-[260px] w-full" />
        </Card>
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        {Array.from({ length: 2 }).map((_, index) => (
          <Card key={index} className="p-6">
            <Skeleton className="h-5 w-32" />
            <Skeleton className="mt-4 h-[180px] w-full" />
          </Card>
        ))}
      </div>
    </div>
  );
}

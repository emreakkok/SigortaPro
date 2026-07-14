import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ChartTooltip } from "@/features/dashboard/components/ChartTooltip";
import type { BranchDistributionPoint } from "@/features/dashboard/types/dashboard.types";
import { INSURANCE_BRANCH_LABELS } from "@/shared/types/insurance.types";
import { formatCompactCurrency } from "@/shared/utils/format";

/**
 * Branş bazlı prim dağılımı: yatay bar (büyüklük karşılaştırması → tek hue; kimlik
 * kategori ekseninde taşınır, renkte değil). Büyükten küçüğe sıralanır; poliçe adedi
 * tooltip'te gösterilir.
 */
export function BranchDistributionChart({ data }: { data: BranchDistributionPoint[] }) {
  if (data.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-muted-foreground">
        Görüntülenecek dağılım verisi yok.
      </p>
    );
  }

  const chartData = [...data]
    .sort((a, b) => b.premiumTotal - a.premiumTotal)
    .map((point) => ({
      label: INSURANCE_BRANCH_LABELS[point.branch],
      premiumTotal: point.premiumTotal,
      policyCount: point.policyCount,
    }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart
        data={chartData}
        layout="vertical"
        margin={{ top: 8, right: 16, bottom: 0, left: 8 }}
      >
        <CartesianGrid horizontal={false} stroke="hsl(var(--border))" strokeOpacity={0.6} />
        <XAxis
          type="number"
          tickLine={false}
          axisLine={false}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
          tickFormatter={(value: number) => formatCompactCurrency(value)}
        />
        <YAxis
          type="category"
          dataKey="label"
          tickLine={false}
          axisLine={false}
          width={88}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
        />
        <Tooltip content={<ChartTooltip />} cursor={{ fill: "hsl(var(--accent))", opacity: 0.5 }} />
        <Bar
          dataKey="premiumTotal"
          fill="hsl(var(--primary))"
          radius={[0, 4, 4, 0]}
          maxBarSize={24}
        />
      </BarChart>
    </ResponsiveContainer>
  );
}

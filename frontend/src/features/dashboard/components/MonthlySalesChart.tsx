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
import type { MonthlySalesPoint } from "@/features/dashboard/types/dashboard.types";
import { formatCompactCurrency, formatMonthLabel } from "@/shared/utils/format";

/**
 * Aylık prim üretimi trendi (son 12 ay): tek serili sütun grafik. Tek seri olduğundan
 * tek renk (tema primary) kullanılır ve ayrı legend gerekmez — başlık seriyi adlandırır.
 * Poliçe adedi tooltip'te gösterilir (ikinci eksen açılmaz — tek eksen kuralı).
 */
export function MonthlySalesChart({ data }: { data: MonthlySalesPoint[] }) {
  if (data.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-muted-foreground">
        Görüntülenecek satış verisi yok.
      </p>
    );
  }

  const chartData = data.map((point) => ({
    label: formatMonthLabel(point.year, point.month),
    premiumTotal: point.premiumTotal,
    policyCount: point.policyCount,
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={chartData} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
        <CartesianGrid vertical={false} stroke="hsl(var(--border))" strokeOpacity={0.6} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
        />
        <YAxis
          tickLine={false}
          axisLine={false}
          width={72}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
          tickFormatter={(value: number) => formatCompactCurrency(value)}
        />
        <Tooltip content={<ChartTooltip />} cursor={{ fill: "hsl(var(--accent))", opacity: 0.5 }} />
        <Bar
          dataKey="premiumTotal"
          fill="hsl(var(--primary))"
          radius={[4, 4, 0, 0]}
          maxBarSize={32}
        />
      </BarChart>
    </ResponsiveContainer>
  );
}

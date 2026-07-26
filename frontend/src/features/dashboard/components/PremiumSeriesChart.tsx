import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { ChartTooltip } from "@/features/dashboard/components/ChartTooltip";
import {
  PremiumGranularity,
  type PremiumSeriesPoint,
} from "@/features/dashboard/types/dashboard.types";
import { APP_TIME_ZONE, formatCompactCurrency } from "@/shared/utils/format";

// TIMEZONE: kova başlangıçları backend'den UTC instant ("Z") gelir; eksen etiketleri Europe/Istanbul ile gösterilir.
const hourFormatter = new Intl.DateTimeFormat("tr-TR", {
  hour: "2-digit",
  minute: "2-digit",
  timeZone: APP_TIME_ZONE,
});
const dayFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "short",
  timeZone: APP_TIME_ZONE,
});
const monthFormatter = new Intl.DateTimeFormat("tr-TR", {
  month: "short",
  year: "2-digit",
  timeZone: APP_TIME_ZONE,
});

/** Kova başlangıcını, seçilen granülerliğe uygun kısa eksen etiketine çevirir. */
function bucketLabel(iso: string, granularity: PremiumGranularity): string {
  const date = new Date(iso);
  if (granularity === PremiumGranularity.Hourly) {
    return hourFormatter.format(date);
  }
  return granularity === PremiumGranularity.Monthly
    ? monthFormatter.format(date)
    : dayFormatter.format(date);
}

/**
 * Prim üretimi zaman serisi (ADR-052). Kova genişliği (saat/gün/ay) seçilen tarih aralığından backend'de
 * türetilir; böylece "Bugün" tek noktalı, uzun aralıklar da yüzlerce noktalı olmaz.
 * Veri gerçek `Policy.TotalPremium` toplamıdır ve **üretim tarihine (Policy.CreatedAt)** göre gruplanır —
 * poliçe listesindeki `StartDate` (teminat başlangıcı) ile bilinçli olarak farklıdır; burada ölçülen
 * satış/üretim performansıdır. Renkler token'lardan gelir → Dark Mode otomatik.
 */
export function PremiumSeriesChart({
  data,
  granularity,
}: {
  data: PremiumSeriesPoint[];
  granularity: PremiumGranularity;
}) {
  if (data.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-muted-foreground">
        Seçilen dönemde üretilmiş poliçe yok.
      </p>
    );
  }

  const chartData = data.map((point) => ({
    label: bucketLabel(point.bucketStart, granularity),
    premiumTotal: point.premiumTotal,
    policyCount: point.policyCount,
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <AreaChart data={chartData} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
        <defs>
          <linearGradient id="premiumSeriesFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="hsl(var(--primary))" stopOpacity={0.28} />
            <stop offset="100%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} stroke="hsl(var(--border))" strokeOpacity={0.6} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          minTickGap={16}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
        />
        <YAxis
          tickLine={false}
          axisLine={false}
          width={72}
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 12 }}
          tickFormatter={(value: number) => formatCompactCurrency(value)}
        />
        <Tooltip content={<ChartTooltip />} cursor={{ stroke: "hsl(var(--primary))", strokeOpacity: 0.4 }} />
        <Area
          type="monotone"
          dataKey="premiumTotal"
          stroke="hsl(var(--primary))"
          strokeWidth={2}
          fill="url(#premiumSeriesFill)"
          dot={false}
          activeDot={{ r: 4, fill: "hsl(var(--primary))", stroke: "hsl(var(--card))", strokeWidth: 2 }}
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}

import { useQuery } from "@tanstack/react-query";
import {
  getDashboardSummary,
  getPolicyReport,
  getRiskiestCustomers,
} from "@/features/dashboard/services/dashboardApi";
import type {
  DashboardSummaryParams,
  PolicyReportParams,
} from "@/features/dashboard/types/dashboard.types";

export const dashboardQueryKeys = {
  all: ["dashboard"] as const,
  summary: (params: DashboardSummaryParams) => ["dashboard", "summary", params] as const,
  policyReport: (params: PolicyReportParams) => ["dashboard", "policyReport", params] as const,
  riskiestCustomers: (top: number) => ["dashboard", "riskiestCustomers", top] as const,
};

/**
 * Operasyon dashboard'ının tek veri kaynağı. Tarih aralığı query key'in parçasıdır →
 * filtre değiştiğinde TÜM bloklar tek istekle tutarlı biçimde yenilenir (blok başına ayrı çağrı yoktur).
 * `placeholderData` ile filtre değişiminde eski veri korunur → ekran boşalmaz/zıplamaz.
 */
export function useDashboardSummary(params: DashboardSummaryParams) {
  return useQuery({
    queryKey: dashboardQueryKeys.summary(params),
    queryFn: () => getDashboardSummary(params),
    placeholderData: (previous) => previous,
  });
}

/**
 * Tarih aralıklı poliçe raporu (admin poliçe yönetim tablosunun veri kaynağı).
 * `enabled=false` iken (geçersiz tarih aralığı) istek atılmaz.
 */
export function usePolicyReport(params: PolicyReportParams, enabled = true) {
  return useQuery({
    queryKey: dashboardQueryKeys.policyReport(params),
    queryFn: () => getPolicyReport(params),
    enabled,
  });
}

/** En riskli müşteri segmentleri (hasar sayısına göre ilk N). */
export function useRiskiestCustomers(top: number) {
  return useQuery({
    queryKey: dashboardQueryKeys.riskiestCustomers(top),
    queryFn: () => getRiskiestCustomers(top),
  });
}

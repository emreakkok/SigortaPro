import { useQuery } from "@tanstack/react-query";
import {
  getDashboardSummary,
  getPolicyReport,
  getRiskiestCustomers,
} from "@/features/dashboard/services/dashboardApi";
import type { PolicyReportParams } from "@/features/dashboard/types/dashboard.types";

export const dashboardQueryKeys = {
  all: ["dashboard"] as const,
  summary: ["dashboard", "summary"] as const,
  policyReport: (params: PolicyReportParams) => ["dashboard", "policyReport", params] as const,
  riskiestCustomers: (top: number) => ["dashboard", "riskiestCustomers", top] as const,
};

/** Dashboard özet metrikleri (tek çağrı — metrik kartları + grafikler buradan beslenir). */
export function useDashboardSummary() {
  return useQuery({
    queryKey: dashboardQueryKeys.summary,
    queryFn: getDashboardSummary,
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

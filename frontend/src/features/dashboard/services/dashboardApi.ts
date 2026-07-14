import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  CustomerRiskSegment,
  DashboardSummary,
  PolicyReportItem,
  PolicyReportParams,
} from "@/features/dashboard/types/dashboard.types";

/** `GET /dashboard/summary` — özet metrikler + aylık trend + branş dağılımı (tek çağrı). */
export async function getDashboardSummary(): Promise<DashboardSummary> {
  const response = await api.get<DashboardSummary>("/dashboard/summary");
  return response.data;
}

/** `GET /dashboard/reports/policies` — tarih aralıklı poliçe raporu (başlangıç tarihine göre, sayfalı). */
export async function getPolicyReport(
  params: PolicyReportParams,
): Promise<PagedResult<PolicyReportItem>> {
  const response = await api.get<PagedResult<PolicyReportItem>>("/dashboard/reports/policies", {
    params,
  });
  return response.data;
}

/** `GET /dashboard/reports/riskiest-customers` — hasar sayısına göre ilk N müşteri segmenti. */
export async function getRiskiestCustomers(top: number): Promise<CustomerRiskSegment[]> {
  const response = await api.get<CustomerRiskSegment[]>("/dashboard/reports/riskiest-customers", {
    params: { top },
  });
  return response.data;
}

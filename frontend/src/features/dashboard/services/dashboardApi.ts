import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type {
  CustomerRiskSegment,
  DashboardSummary,
  DashboardSummaryParams,
  PolicyReportItem,
  PolicyReportParams,
} from "@/features/dashboard/types/dashboard.types";

/**
 * `GET /dashboard/summary` — operasyon dashboard'ının TÜM blokları tek çağrıda:
 * dönem KPI'ları + karşılaştırma, aksiyon merkezi, prim serisi, satış hunisi, branş performansı,
 * hasar operasyonu, portföy. Filtre değiştiğinde yalnızca bu istek yenilenir.
 */
export async function getDashboardSummary(
  params: DashboardSummaryParams = {},
): Promise<DashboardSummary> {
  const response = await api.get<DashboardSummary>("/dashboard/summary", { params });
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

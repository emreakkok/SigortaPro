import type {
  InsuranceBranch,
  PolicyStatus,
} from "@/shared/types/insurance.types";

/** Aylık satış trendi kalemi (backend `MonthlySalesPointDto`). */
export interface MonthlySalesPoint {
  year: number;
  month: number;
  policyCount: number;
  premiumTotal: number;
}

/** Branş bazlı poliçe/prim dağılımı kalemi (backend `BranchDistributionPointDto`). */
export interface BranchDistributionPoint {
  branch: InsuranceBranch;
  policyCount: number;
  premiumTotal: number;
}

/** Dashboard özet metrikleri (backend `DashboardSummaryDto`). Oranlar 0..1 aralığında döner. */
export interface DashboardSummary {
  totalPremiumProduction: number;
  activePolicyCount: number;
  pendingQuoteCount: number;
  pendingClaimCount: number;
  renewalRate: number;
  claimToPremiumRatio: number;
  monthlySales: MonthlySalesPoint[];
  branchDistribution: BranchDistributionPoint[];
}

/** Tarih aralıklı poliçe raporu kalemi (backend `PolicyReportItemDto`). */
export interface PolicyReportItem {
  id: string;
  policyNumber: string;
  customerFullName: string;
  branch: InsuranceBranch;
  status: PolicyStatus;
  startDate: string;
  endDate: string;
  totalPremium: number;
}

/** `GET /dashboard/reports/policies` sorgu parametreleri (From/To zorunlu, dahil). */
export interface PolicyReportParams {
  from: string;
  to: string;
  page?: number;
  pageSize?: number;
}

/** En riskli müşteri segmenti kalemi (backend `CustomerRiskSegmentDto`). */
export interface CustomerRiskSegment {
  customerId: string;
  fullName: string;
  claimCount: number;
  totalClaimAmount: number;
}

import type {
  InsuranceBranch,
  PolicyStatus,
} from "@/shared/types/insurance.types";

/** Prim serisinin kova genişliği (backend `PremiumGranularityDto` — sayısal enum). */
export const PremiumGranularity = {
  Hourly: 0,
  Daily: 1,
  Monthly: 2,
} as const;
export type PremiumGranularity = (typeof PremiumGranularity)[keyof typeof PremiumGranularity];

/** Bir dönemin operasyon sayaçları (backend `DashboardPeriodStatsDto`). */
export interface DashboardPeriodStats {
  newCustomers: number;
  newQuotes: number;
  newPolicies: number;
  newClaims: number;
  premiumProduction: number;
}

/**
 * Önceki eşit uzunluktaki döneme göre oransal değişim (0.18 = +%18).
 * **null** = önceki dönem 0 → değişim tanımsız (yanıltıcı "+%100" gösterilmez).
 */
export interface DashboardDelta {
  premiumProduction: number | null;
  newPolicies: number | null;
  newQuotes: number | null;
  newCustomers: number | null;
}

/** Aksiyon merkezi sayaçları (backend `OperationalAlertsDto`). */
export interface OperationalAlerts {
  pendingQuotes: number;
  pendingClaims: number;
  upcomingRenewals: number;
  upcomingRenewalWindowDays: number;
  failedPayments: number;
}

/** Portföyün anlık durumu (backend `PortfolioDto`). `lossRatio` prim yoksa null. */
export interface DashboardPortfolio {
  activePolicyCount: number;
  totalCustomerCount: number;
  lifetimePremiumProduction: number;
  paidClaimAmount: number;
  lossRatio: number | null;
}

/** Satış hunisi (backend `QuoteFunnelDto`). `conversionRate` teklif yoksa null. */
export interface QuoteFunnel {
  created: number;
  approved: number;
  purchased: number;
  expired: number;
  rejected: number;
  conversionRate: number | null;
}

/** Prim üretimi serisi noktası (backend `PremiumSeriesPointDto`) — üretim tarihine (Policy.CreatedAt) göre. */
export interface PremiumSeriesPoint {
  bucketStart: string;
  policyCount: number;
  premiumTotal: number;
}

/** Branş performansı (backend `BranchPerformanceDto`) — teklif kohortu; `conversionRate` teklif yoksa null. */
export interface BranchPerformance {
  branch: InsuranceBranch;
  quoteCount: number;
  purchasedCount: number;
  premiumTotal: number;
  conversionRate: number | null;
}

/** Hasar operasyonu durum kırılımı (backend `ClaimOperationDto`). */
export interface ClaimOperation {
  submitted: number;
  underReview: number;
  approved: number;
  rejected: number;
  paid: number;
  paidAmount: number;
  estimatedAmount: number;
}

/**
 * Operasyon dashboard'ının tüm blokları (backend `DashboardSummaryDto`).
 * Oranlar 0..1 ondalıktır; güvenilir hesaplanamayanlar **null** döner.
 */
export interface DashboardSummary {
  from: string;
  to: string;
  granularity: PremiumGranularity;
  current: DashboardPeriodStats;
  previous: DashboardPeriodStats;
  deltas: DashboardDelta;
  alerts: OperationalAlerts;
  portfolio: DashboardPortfolio;
  funnel: QuoteFunnel;
  premiumSeries: PremiumSeriesPoint[];
  branchPerformance: BranchPerformance[];
  claims: ClaimOperation;
  renewalRate: number | null;
}

/** `GET /dashboard/summary` sorgu parametreleri (verilmezse backend son 30 günü kullanır). */
export interface DashboardSummaryParams {
  from?: string;
  to?: string;
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
  /** Müşteri kimliği (additive) — aynı isimli müşterileri ayırt etmek için telefon + stabil id. */
  customerId?: string;
  customerPhone?: string | null;
}

/** `GET /dashboard/reports/policies` sorgu parametreleri (From/To zorunlu, dahil). */
export interface PolicyReportParams {
  from: string;
  to: string;
  page?: number;
  pageSize?: number;
  /** Müşteri adı/soyadı/tam adı, telefon (format bağımsız) veya poliçe numarası. */
  search?: string;
}

/** En riskli müşteri segmenti kalemi (backend `CustomerRiskSegmentDto`). */
export interface CustomerRiskSegment {
  customerId: string;
  fullName: string;
  claimCount: number;
  totalClaimAmount: number;
}

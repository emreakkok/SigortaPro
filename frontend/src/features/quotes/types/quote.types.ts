import type {
  CoveragePackage,
  InsuranceBranch,
  QuoteStatus,
  RiskScore,
} from "@/shared/types/insurance.types";

/** Prim dökümünde tek bir risk faktörü (backend `PricingBreakdownItem`). */
export interface PricingBreakdownItem {
  factor: string;
  multiplier: number;
  description: string;
}

/** Teklifin bir teminat kalemi (backend `QuoteCoverageDto`). */
export interface QuoteCoverage {
  name: string;
  description: string | null;
  limit: number;
}

/** Teklifin risk objesi özeti (backend `QuoteRiskObjectDto`). */
export interface QuoteRiskObject {
  kind: string;
  display: string;
}

/** Karşılaştırmadaki tek paket alternatifi (backend `QuotePackageDto`) — henüz oluşturulmamış önizleme. */
export interface QuotePackage {
  coveragePackage: CoveragePackage;
  riskScore: RiskScore;
  totalPremium: number;
  coverages: QuoteCoverage[];
  premiumBreakdown: PricingBreakdownItem[];
}

/** Paket karşılaştırma sonucu (backend `QuoteComparisonDto`). */
export interface QuoteComparison {
  branch: InsuranceBranch;
  productName: string;
  riskObject: QuoteRiskObject;
  packages: QuotePackage[];
}

/** Teklif detayı (backend `QuoteDto`). */
export interface Quote {
  id: string;
  customerId: string;
  branch: InsuranceBranch;
  productName: string;
  status: QuoteStatus;
  coveragePackage: CoveragePackage;
  riskScore: RiskScore;
  basePremium: number;
  totalPremium: number;
  validUntil: string | null;
  createdAt: string;
  riskObject: QuoteRiskObject;
  coverages: QuoteCoverage[];
  premiumBreakdown: PricingBreakdownItem[];
}

/** Teklif listesi özeti (backend `QuoteSummaryDto`). */
export interface QuoteSummary {
  id: string;
  branch: InsuranceBranch;
  productName: string;
  status: QuoteStatus;
  coveragePackage: CoveragePackage;
  totalPremium: number;
  validUntil: string | null;
  createdAt: string;
}

/** `POST /quotes` istek gövdesi (backend `CreateQuoteCommand`). Enum'lar sayısal gönderilir. */
export interface CreateQuoteRequest {
  branch: InsuranceBranch;
  vehicleId: string | null;
  propertyId: string | null;
  coveragePackage: CoveragePackage;
}

/** `GET /quotes` sorgu parametreleri. */
export interface QuoteListParams {
  page?: number;
  pageSize?: number;
  status?: QuoteStatus;
  branch?: InsuranceBranch;
}

/** `GET /quotes/compare` sorgu parametreleri. */
export interface QuoteComparisonParams {
  branch: InsuranceBranch;
  vehicleId?: string;
  propertyId?: string;
}

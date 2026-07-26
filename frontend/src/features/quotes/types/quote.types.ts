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
  /** Sağlıkta "başkası adına" teklifte sigortalı özeti (ADR-041); kendisi için null. */
  insuredPerson?: QuoteInsuredPerson | null;
  /** Müşteri (Sigorta Ettiren) kimliği — admin detayında ad + telefon özeti (additive). */
  customerFullName?: string;
  customerPhone?: string | null;
}

/** "Başkası adına" sağlık teklifinde sigortalı özeti (backend `QuoteInsuredPersonDto` — TCKN maskeli). */
export interface QuoteInsuredPerson {
  fullName: string;
  maskedTckn: string;
  birthDate: string;
  relationship: string;
}

/** Sigortalı beyanı (backend `InsuredPersonInput` — ADR-041). */
export interface InsuredPersonRequest {
  firstName: string;
  lastName: string;
  tckn: string;
  /** ISO tarih (input type="date" → "YYYY-MM-DD"). */
  birthDate: string;
  phoneNumber: string;
  relationship: string;
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
  /** Müşteri kimliği (additive) — admin listesinde aynı isimli müşterileri ayırt etmek için ad + telefon. */
  customerId: string;
  customerFullName: string;
  customerPhone: string | null;
}

/** `POST /quotes` istek gövdesi (backend `CreateQuoteCommand`). Enum'lar sayısal gönderilir. */
export interface CreateQuoteRequest {
  branch: InsuranceBranch;
  vehicleId: string | null;
  propertyId: string | null;
  coveragePackage: CoveragePackage;
  /** Sağlıkta "başkası adına" sigortalı beyanı (ADR-041); kendisi için gönderilmez. */
  insuredPerson?: InsuredPersonRequest | null;
  /** Sağlıkta sigara kullanım beyanı (ADR-054) — zorunlu; diğer branşlarda gönderilmez. */
  isSmoker?: boolean | null;
}

/** `GET /quotes` sorgu parametreleri. */
export interface QuoteListParams {
  page?: number;
  pageSize?: number;
  status?: QuoteStatus;
  branch?: InsuranceBranch;
  /** Müşteri adı/soyadı/tam adı veya telefon (format bağımsız) — admin araması. */
  search?: string;
}

/** `GET /quotes/compare` sorgu parametreleri. */
export interface QuoteComparisonParams {
  branch: InsuranceBranch;
  vehicleId?: string;
  propertyId?: string;
  /** Sağlıkta "başkası adına" önizleme için sigortalının doğum tarihi (ADR-041). */
  insuredBirthDate?: string;
  /**
   * Sağlıkta sigara beyanı (ADR-056) — önizlemenin, oluşturulacak teklifle AYNI fiyatı göstermesi için
   * zorunludur. Gönderilmezse backend 400 döner (kural teklif oluşturmayla birebir aynıdır).
   */
  isSmoker?: boolean | null;
}

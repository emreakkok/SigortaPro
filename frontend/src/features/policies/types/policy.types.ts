import type {
  QuoteCoverage,
  QuoteInsuredPerson,
  QuoteRiskObject,
} from "@/features/quotes/types/quote.types";
import type {
  CoveragePackage,
  InsuranceBranch,
  PolicyStatus,
} from "@/shared/types/insurance.types";

/** "Poliçelerim" liste kalemi (backend `PolicyListItemDto`). */
export interface PolicyListItem {
  id: string;
  policyNumber: string;
  branch: InsuranceBranch;
  productName: string;
  status: PolicyStatus;
  startDate: string;
  endDate: string;
  totalPremium: number;
}

/** Poliçe detayı (backend `PolicyDetailDto`). Teminatlar/risk objesi teklif detayıyla aynı şekildedir. */
export interface PolicyDetail {
  id: string;
  policyNumber: string;
  branch: InsuranceBranch;
  productName: string;
  status: PolicyStatus;
  coveragePackage: CoveragePackage;
  startDate: string;
  endDate: string;
  totalPremium: number;
  quoteId: string;
  riskObject: QuoteRiskObject;
  coverages: QuoteCoverage[];
  /** Müşteri (Sigorta Ettiren) kimliği — admin detayında ad + telefon özeti (additive). */
  customerId?: string;
  customerFullName?: string;
  customerPhone?: string | null;
  /** Sağlıkta "başkası adına" poliçede Sigortalı özeti; Ettiren = müşteri. Değilse null. */
  insuredPerson?: QuoteInsuredPerson | null;
}

/** `GET /policies` sorgu parametreleri. */
export interface PolicyListParams {
  page?: number;
  pageSize?: number;
  status?: PolicyStatus;
}

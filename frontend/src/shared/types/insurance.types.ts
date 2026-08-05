import type { BadgeProps } from "@/shared/components/Badge";

/**
 * Backend Domain enum'larının frontend karşılığı. Backend enum'ları JSON'da **sayısal
 * indeksle** serileştirir (JsonStringEnumConverter kayıtlı değil); bu yüzden değerler
 * numaradır ve API'ye de numara olarak gönderilir. İndeksler backend enum sırasıyla birebir.
 */
export const InsuranceBranch = {
  Kasko: 0,
  Trafik: 1,
  Konut: 2,
  Dask: 3,
  Saglik: 4,
} as const;
export type InsuranceBranch = (typeof InsuranceBranch)[keyof typeof InsuranceBranch];

export const INSURANCE_BRANCH_LABELS: Record<InsuranceBranch, string> = {
  [InsuranceBranch.Kasko]: "Kasko",
  [InsuranceBranch.Trafik]: "Trafik",
  [InsuranceBranch.Konut]: "Konut",
  [InsuranceBranch.Dask]: "DASK",
  [InsuranceBranch.Saglik]: "Sağlık",
};

export const INSURANCE_BRANCH_DESCRIPTIONS: Record<InsuranceBranch, string> = {
  [InsuranceBranch.Kasko]: "Aracınız için kapsamlı hasar güvencesi.",
  [InsuranceBranch.Trafik]: "Zorunlu trafik sigortası — üçüncü şahıslara karşı.",
  [InsuranceBranch.Konut]: "Konutunuz ve eşyalarınız için güvence.",
  [InsuranceBranch.Dask]: "Zorunlu deprem sigortası.",
  [InsuranceBranch.Saglik]: "Ayakta ve yatarak tedavi giderleri.",
};

/** Kasko/Trafik → araç, Konut/DASK → konut, Sağlık → risk objesi gerekmez. */
export function branchRiskKind(branch: InsuranceBranch): "vehicle" | "property" | "none" {
  if (branch === InsuranceBranch.Kasko || branch === InsuranceBranch.Trafik) {
    return "vehicle";
  }
  if (branch === InsuranceBranch.Konut || branch === InsuranceBranch.Dask) {
    return "property";
  }
  return "none";
}

/** Aracın kullanım amacı (backend `VehicleUsage` — ADR-057). Kasko/Trafik primini etkiler. */
export const VehicleUsage = {
  Hususi: 0,
  Ticari: 1,
  Taksi: 2,
} as const;
export type VehicleUsage = (typeof VehicleUsage)[keyof typeof VehicleUsage];

export const VEHICLE_USAGE_LABELS: Record<VehicleUsage, string> = {
  [VehicleUsage.Hususi]: "Hususi",
  [VehicleUsage.Ticari]: "Ticari",
  [VehicleUsage.Taksi]: "Taksi",
};

/** Kullanıcının bilinçli seçim yapabilmesi için sade Türkçe açıklamalar. */
export const VEHICLE_USAGE_DESCRIPTIONS: Record<VehicleUsage, string> = {
  [VehicleUsage.Hususi]: "Kişisel kullanım — işe gidiş-geliş ve günlük ihtiyaçlar.",
  [VehicleUsage.Ticari]: "İş amaçlı kullanım — esnaf/şirket aracı, yük veya hizmet taşıma.",
  [VehicleUsage.Taksi]: "Ticari yolcu taşımacılığı — taksi, transfer ve benzeri.",
};

export const CoveragePackage = {
  Standart: 0,
  Genisletilmis: 1,
  Premium: 2,
} as const;
export type CoveragePackage = (typeof CoveragePackage)[keyof typeof CoveragePackage];

export const COVERAGE_PACKAGE_LABELS: Record<CoveragePackage, string> = {
  [CoveragePackage.Standart]: "Standart",
  [CoveragePackage.Genisletilmis]: "Genişletilmiş",
  [CoveragePackage.Premium]: "Premium",
};

export const QuoteStatus = {
  Draft: 0,
  Priced: 1,
  Approved: 2,
  Purchased: 3,
  Expired: 4,
  Rejected: 5,
} as const;
export type QuoteStatus = (typeof QuoteStatus)[keyof typeof QuoteStatus];

export const QUOTE_STATUS_LABELS: Record<QuoteStatus, string> = {
  [QuoteStatus.Draft]: "Taslak",
  [QuoteStatus.Priced]: "Fiyatlandı",
  [QuoteStatus.Approved]: "Onaylandı",
  [QuoteStatus.Purchased]: "Satın Alındı",
  [QuoteStatus.Expired]: "Süresi Doldu",
  [QuoteStatus.Rejected]: "Reddedildi",
};

export const QUOTE_STATUS_BADGE_VARIANTS: Record<QuoteStatus, BadgeProps["variant"]> = {
  [QuoteStatus.Draft]: "secondary",
  [QuoteStatus.Priced]: "default",
  [QuoteStatus.Approved]: "success",
  [QuoteStatus.Purchased]: "success",
  [QuoteStatus.Expired]: "secondary",
  [QuoteStatus.Rejected]: "destructive",
};

/**
 * Teklifin oluşturulma kaynağı (backend `QuoteSource` — türetilmiş). SelfService: müşteri kendi oluşturdu;
 * AgentAssisted: acente personeli müşteri adına oluşturdu. Müşteri yüzeyinde personel kimliği gösterilmez.
 */
export const QuoteSource = {
  SelfService: 0,
  AgentAssisted: 1,
} as const;
export type QuoteSource = (typeof QuoteSource)[keyof typeof QuoteSource];

/** Müşteri yüzeyi etiketi ("Oluşturan: …"): kendi mi yaptı yoksa acente mi. */
export const QUOTE_SOURCE_CUSTOMER_LABELS: Record<QuoteSource, string> = {
  [QuoteSource.SelfService]: "Kendiniz",
  [QuoteSource.AgentAssisted]: "Acente",
};

/** Personel/admin yüzeyi etiketi (kanal görünümü). */
export const QUOTE_SOURCE_STAFF_LABELS: Record<QuoteSource, string> = {
  [QuoteSource.SelfService]: "Online",
  [QuoteSource.AgentAssisted]: "Acente",
};

export const QUOTE_SOURCE_BADGE_VARIANTS: Record<QuoteSource, BadgeProps["variant"]> = {
  [QuoteSource.SelfService]: "secondary",
  [QuoteSource.AgentAssisted]: "default",
};

export const PolicyStatus = {
  Active: 0,
  Expired: 1,
  Cancelled: 2,
} as const;
export type PolicyStatus = (typeof PolicyStatus)[keyof typeof PolicyStatus];

export const POLICY_STATUS_LABELS: Record<PolicyStatus, string> = {
  [PolicyStatus.Active]: "Aktif",
  [PolicyStatus.Expired]: "Süresi Doldu",
  [PolicyStatus.Cancelled]: "İptal Edildi",
};

export const POLICY_STATUS_BADGE_VARIANTS: Record<PolicyStatus, BadgeProps["variant"]> = {
  [PolicyStatus.Active]: "success",
  [PolicyStatus.Expired]: "secondary",
  [PolicyStatus.Cancelled]: "destructive",
};

export const ClaimStatus = {
  Submitted: 0,
  UnderReview: 1,
  Approved: 2,
  Rejected: 3,
  Paid: 4,
} as const;
export type ClaimStatus = (typeof ClaimStatus)[keyof typeof ClaimStatus];

export const CLAIM_STATUS_LABELS: Record<ClaimStatus, string> = {
  [ClaimStatus.Submitted]: "Bildirildi",
  [ClaimStatus.UnderReview]: "İncelemede",
  [ClaimStatus.Approved]: "Onaylandı",
  [ClaimStatus.Rejected]: "Reddedildi",
  [ClaimStatus.Paid]: "Ödendi",
};

export const CLAIM_STATUS_BADGE_VARIANTS: Record<ClaimStatus, BadgeProps["variant"]> = {
  [ClaimStatus.Submitted]: "default",
  [ClaimStatus.UnderReview]: "warning",
  [ClaimStatus.Approved]: "success",
  [ClaimStatus.Rejected]: "destructive",
  [ClaimStatus.Paid]: "success",
};

export const PaymentStatus = {
  Pending: 0,
  Successful: 1,
  Failed: 2,
} as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: "Beklemede",
  [PaymentStatus.Successful]: "Başarılı",
  [PaymentStatus.Failed]: "Başarısız",
};

export const PAYMENT_STATUS_BADGE_VARIANTS: Record<PaymentStatus, BadgeProps["variant"]> = {
  [PaymentStatus.Pending]: "secondary",
  [PaymentStatus.Successful]: "success",
  [PaymentStatus.Failed]: "destructive",
};

export const RiskScore = {
  Low: 0,
  Medium: 1,
  High: 2,
} as const;
export type RiskScore = (typeof RiskScore)[keyof typeof RiskScore];

export const RISK_SCORE_LABELS: Record<RiskScore, string> = {
  [RiskScore.Low]: "Düşük Risk",
  [RiskScore.Medium]: "Orta Risk",
  [RiskScore.High]: "Yüksek Risk",
};

export const RISK_SCORE_BADGE_VARIANTS: Record<RiskScore, BadgeProps["variant"]> = {
  [RiskScore.Low]: "success",
  [RiskScore.Medium]: "warning",
  [RiskScore.High]: "destructive",
};

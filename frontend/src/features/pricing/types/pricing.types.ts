import type { BadgeProps } from "@/shared/components/Badge";
import type { CoveragePackage, InsuranceBranch } from "@/shared/types/insurance.types";

/** Tarife versiyonu yaşam döngüsü (backend `PricingVersionStatus`). */
export const PricingVersionStatus = {
  Draft: 0,
  Active: 1,
  Archived: 2,
} as const;
export type PricingVersionStatus = (typeof PricingVersionStatus)[keyof typeof PricingVersionStatus];

export const PRICING_STATUS_LABELS: Record<PricingVersionStatus, string> = {
  [PricingVersionStatus.Draft]: "Taslak",
  [PricingVersionStatus.Active]: "Aktif",
  [PricingVersionStatus.Archived]: "Arşiv",
};

export const PRICING_STATUS_BADGE_VARIANTS: Record<PricingVersionStatus, BadgeProps["variant"]> = {
  [PricingVersionStatus.Draft]: "warning",
  [PricingVersionStatus.Active]: "success",
  [PricingVersionStatus.Archived]: "secondary",
};

export interface PricingBranchRate {
  branch: InsuranceBranch;
  basePremium: number;
  previousBasePremium: number | null;
}

export interface PackageFactor {
  package: CoveragePackage;
  premiumFactor: number;
}

export interface CityCoefficient {
  city: string;
  coefficient: number;
}

/**
 * Baz prim dışındaki TÜM çarpanlar (backend `PricingRuleSetDto`). Bantlı faktörler SIRALI çarpan
 * listeleridir; index sözleşmesi backend `PricingRuleSet` ile birebir (frontend etiketleri buna göre sabittir).
 */
export interface PricingRuleSet {
  packagePremiumFactors: PackageFactor[];
  cityRiskCoefficients: CityCoefficient[];
  defaultCityRiskCoefficient: number;
  renewalDiscountFactor: number;
  driverAgeFactors: number[];
  vehicleAgeFactors: number[];
  enginePowerFactors: number[];
  vehicleUsageFactors: number[];
  bonusMalusFactors: number[];
  buildingAgeFactors: number[];
  squareMetersFactors: number[];
  earthquakeZoneFactors: number[];
  healthAgeFactors: number[];
  smokerSurcharge: number;
}

/** Tarife versiyonu (backend `PricingVersionDto`). Aktif/arşiv değişmez; yalnızca taslak düzenlenir. */
export interface PricingVersion {
  id: string;
  versionNumber: number;
  name: string | null;
  status: PricingVersionStatus;
  effectiveFrom: string;
  effectiveTo: string | null;
  activatedAt: string | null;
  note: string | null;
  createdByName: string | null;
  createdAt: string;
  isCurrent: boolean;
  isBaseline: boolean;
  rates: PricingBranchRate[];
  ruleSet: PricingRuleSet;
}

/** `POST /pricing/versions` istek gövdesi (taslak oluşturma). İsim zorunludur. */
export interface CreatePricingDraftRequest {
  name: string;
}

/** `PUT /pricing/versions/{id}` istek gövdesi (taslak düzenleme). */
export interface UpdatePricingDraftRequest {
  name: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  note: string | null;
  rates: { branch: InsuranceBranch; basePremium: number }[];
  packagePremiumFactors: { package: CoveragePackage; premiumFactor: number }[];
  cityRiskCoefficients: { city: string; coefficient: number }[];
  defaultCityRiskCoefficient: number;
  renewalDiscountFactor: number;
  driverAgeFactors: number[];
  vehicleAgeFactors: number[];
  enginePowerFactors: number[];
  vehicleUsageFactors: number[];
  bonusMalusFactors: number[];
  buildingAgeFactors: number[];
  squareMetersFactors: number[];
  earthquakeZoneFactors: number[];
  healthAgeFactors: number[];
  smokerSurcharge: number;
}

/** Bantlı faktörlerin sabit etiketleri (backend index sözleşmesiyle birebir sırada). */
export type BandKey =
  | "driverAgeFactors"
  | "vehicleAgeFactors"
  | "enginePowerFactors"
  | "vehicleUsageFactors"
  | "bonusMalusFactors"
  | "buildingAgeFactors"
  | "squareMetersFactors"
  | "earthquakeZoneFactors"
  | "healthAgeFactors";

export const BAND_FACTOR_LABELS: Record<BandKey, { title: string; labels: string[] }> = {
  driverAgeFactors: { title: "Sürücü Yaşı", labels: ["25 yaş altı", "25–65 yaş", "65 yaş üstü"] },
  vehicleAgeFactors: { title: "Araç Yaşı", labels: ["0–3 yaş", "4–10 yaş", "10 yaş üstü"] },
  enginePowerFactors: {
    title: "Motor Gücü",
    labels: ["≤100 HP", "101–160 HP", "161–240 HP", "240 HP üstü"],
  },
  vehicleUsageFactors: { title: "Kullanım Amacı", labels: ["Hususi", "Ticari", "Taksi"] },
  bonusMalusFactors: {
    title: "Hasarsızlık Basamağı",
    labels: ["−3", "−2", "−1", "0", "+1", "+2", "+3", "+4", "+5", "+6"],
  },
  buildingAgeFactors: {
    title: "Bina Yaşı",
    labels: ["0–5 yaş", "6–20 yaş", "21–40 yaş", "40 yaş üstü"],
  },
  squareMetersFactors: {
    title: "Metrekare",
    labels: ["≤75 m²", "76–120 m²", "121–200 m²", "200 m² üstü"],
  },
  earthquakeZoneFactors: {
    title: "Deprem Bölgesi",
    labels: ["1. derece", "2. derece", "3. derece", "4. derece", "5. derece", "Bilinmeyen"],
  },
  healthAgeFactors: {
    title: "Yaş Bandı",
    labels: ["0–17", "18–30", "31–45", "46–60", "60 üstü"],
  },
};

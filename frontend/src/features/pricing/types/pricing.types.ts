import type { InsuranceBranch } from "@/shared/types/insurance.types";

/** Tarife versiyonundaki tek branşın baz primi (backend `PricingBranchRateDto` — ADR-048). */
export interface PricingBranchRate {
  branch: InsuranceBranch;
  basePremium: number;
  /** Bir önceki versiyondaki değer (ilk versiyonda null) — değişim etkisini göstermek için. */
  previousBasePremium: number | null;
}

/** Tarife versiyonu (backend `PricingVersionDto` — ADR-048). Versiyonlar değişmezdir. */
export interface PricingVersion {
  id: string;
  versionNumber: number;
  effectiveFrom: string;
  note: string | null;
  createdByName: string | null;
  createdAt: string;
  /** Şu an yürürlükte olan versiyon. */
  isCurrent: boolean;
  /** Geçerlilik tarihi gelecekte — henüz yürürlüğe girmedi. */
  isScheduled: boolean;
  /**
   * Yayınlanmış bir versiyon değil, sistemin yerleşik (kod-sabit) baz tarifesi (v0 — ADR-049).
   * `id`/`effectiveFrom`/`createdByName` anlamsızdır; UI bu satırı özel biçimde gösterir.
   */
  isBaseline: boolean;
  rates: PricingBranchRate[];
}

/** `POST /pricing/versions` istek gövdesi. */
export interface CreatePricingVersionRequest {
  effectiveFrom: string;
  note: string | null;
  rates: { branch: InsuranceBranch; basePremium: number }[];
}

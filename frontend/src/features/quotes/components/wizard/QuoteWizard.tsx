import { useState } from "react";
import { BranchStep } from "@/features/quotes/components/wizard/BranchStep";
import { ComparisonStep } from "@/features/quotes/components/wizard/ComparisonStep";
import { RiskObjectStep } from "@/features/quotes/components/wizard/RiskObjectStep";
import type { InsuredPersonRequest } from "@/features/quotes/types/quote.types";
import type { CustomerProfile } from "@/features/profile/types/profile.types";
import { cn } from "@/shared/lib/utils";
import { branchRiskKind, type InsuranceBranch } from "@/shared/types/insurance.types";

type WizardStep = "branch" | "risk" | "comparison";

const STEP_LABELS: { key: WizardStep; label: string }[] = [
  { key: "branch", label: "Branş" },
  { key: "risk", label: "Risk Bilgileri" },
  { key: "comparison", label: "Paketler" },
];

interface QuoteWizardProps {
  /** Risk objelerinin (araç/konut) okunacağı profil: müşteri kendi profili ya da seçili müşterinin profili. */
  profile: CustomerProfile;
  /**
   * Teklifin oluşturulacağı hedef müşteri. null = self-servis (oturum sahibi müşteri kendi teklifini alır);
   * dolu = acente destekli (personel bu müşteri ADINA oluşturur). Aynı sihirbaz her iki akışta kullanılır.
   */
  customerId: string | null;
  /** Teklif oluşturulunca ilgili detay/rota hedefine yönlendirmek için (müşteri portalı vs. acente paneli). */
  onCreated: (quoteId: string) => void;
}

/**
 * Çok adımlı teklif sihirbazının YENİDEN KULLANILABİLİR gövdesi: branş → risk objesi → anlık prim/paket
 * karşılaştırma → teklif. Hem müşteri self-servis akışında (QuoteWizardPage) hem de acente destekli akışta
 * (personel müşteri adına — AdminCustomerQuoteWizardPage) aynı bileşen kullanılır (tek sihirbaz, kod tekrarı yok).
 */
export function QuoteWizard({ profile, customerId, onCreated }: QuoteWizardProps) {
  const [step, setStep] = useState<WizardStep>("branch");
  const [branch, setBranch] = useState<InsuranceBranch | null>(null);
  const [riskObjectId, setRiskObjectId] = useState<string | null>(null);
  // Sağlıkta "başkası adına" sigortalı beyanı (ADR-041); null = kendim/müşterinin kendisi için.
  const [insuredPerson, setInsuredPerson] = useState<InsuredPersonRequest | null>(null);
  // ADR-054: Sağlıkta sigara beyanı; null = henüz beyan edilmedi (varsayılan atanmaz).
  const [isSmoker, setIsSmoker] = useState<boolean | null>(null);

  const handleBranchSelect = (selected: InsuranceBranch) => {
    setBranch(selected);
    // Branş değişince önceki risk objesi/sigortalı seçimi geçersizdir.
    setRiskObjectId(null);
    setInsuredPerson(null);
    setIsSmoker(null);
    setStep("risk");
  };

  const currentIndex = STEP_LABELS.findIndex((s) => s.key === step);

  return (
    <div className="space-y-6">
      <ol className="flex items-center gap-2 text-sm">
        {STEP_LABELS.map((s, index) => (
          <li key={s.key} className="flex items-center gap-2">
            <span
              className={cn(
                "flex h-7 w-7 items-center justify-center rounded-full border text-xs font-semibold",
                index <= currentIndex
                  ? "border-primary bg-primary text-primary-foreground"
                  : "border-border text-muted-foreground",
              )}
            >
              {index + 1}
            </span>
            <span className={cn(index === currentIndex ? "font-medium" : "text-muted-foreground")}>
              {s.label}
            </span>
            {index < STEP_LABELS.length - 1 && <span className="text-muted-foreground">›</span>}
          </li>
        ))}
      </ol>

      {step === "branch" && <BranchStep selected={branch} onSelect={handleBranchSelect} />}

      {step === "risk" && branch !== null && (
        <RiskObjectStep
          branch={branch}
          vehicles={profile.vehicles}
          properties={profile.properties}
          customerId={customerId}
          selectedId={riskObjectId}
          onSelect={setRiskObjectId}
          insuredPerson={insuredPerson}
          onInsuredPersonChange={setInsuredPerson}
          isSmoker={isSmoker}
          onIsSmokerChange={setIsSmoker}
          onBack={() => setStep("branch")}
          onNext={() => setStep("comparison")}
        />
      )}

      {step === "comparison" && branch !== null && (
        <ComparisonStep
          branch={branch}
          customerId={customerId}
          riskObjectId={branchRiskKind(branch) === "none" ? null : riskObjectId}
          insuredPerson={insuredPerson}
          isSmoker={isSmoker}
          onBack={() => setStep("risk")}
          onCreated={onCreated}
        />
      )}
    </div>
  );
}

import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMyProfile } from "@/features/profile/hooks/useProfile";
import { BranchStep } from "@/features/quotes/components/wizard/BranchStep";
import { ComparisonStep } from "@/features/quotes/components/wizard/ComparisonStep";
import { RiskObjectStep } from "@/features/quotes/components/wizard/RiskObjectStep";
import type { InsuredPersonRequest } from "@/features/quotes/types/quote.types";
import { Alert, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import { branchRiskKind, type InsuranceBranch } from "@/shared/types/insurance.types";

type WizardStep = "branch" | "risk" | "comparison";

const STEP_LABELS: { key: WizardStep; label: string }[] = [
  { key: "branch", label: "Branş" },
  { key: "risk", label: "Risk Bilgileri" },
  { key: "comparison", label: "Paketler" },
];

/** Çok adımlı teklif sihirbazı: branş → risk objesi → anlık prim/paket karşılaştırma → teklif. */
export default function QuoteWizardPage() {
  const navigate = useNavigate();
  const { data: profile, isLoading, isError, error } = useMyProfile();

  const [step, setStep] = useState<WizardStep>("branch");
  const [branch, setBranch] = useState<InsuranceBranch | null>(null);
  const [riskObjectId, setRiskObjectId] = useState<string | null>(null);
  // Sağlıkta "başkası adına" sigortalı beyanı (ADR-041); null = kendim için.
  const [insuredPerson, setInsuredPerson] = useState<InsuredPersonRequest | null>(null);
  // ADR-054: Sağlıkta sigara beyanı; null = henüz beyan edilmedi (varsayılan atanmaz).
  const [isSmoker, setIsSmoker] = useState<boolean | null>(null);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || profile === undefined) {
    return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
  }

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
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Yeni Teklif</h1>
        <p className="text-muted-foreground">Birkaç adımda anlık teklifinizi alın.</p>
      </div>

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
          riskObjectId={branchRiskKind(branch) === "none" ? null : riskObjectId}
          insuredPerson={insuredPerson}
          isSmoker={isSmoker}
          onBack={() => setStep("risk")}
          onCreated={(quoteId) => navigate(`/portal/quotes/${quoteId}`)}
        />
      )}
    </div>
  );
}

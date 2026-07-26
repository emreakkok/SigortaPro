import { useState } from "react";
import { PackageCard } from "@/features/quotes/components/PackageCard";
import { RiskScoreBadge } from "@/features/quotes/components/RiskScoreBadge";
import { useCreateQuote, useQuoteComparison } from "@/features/quotes/hooks/useQuotes";
import type { InsuredPersonRequest, QuoteComparisonParams } from "@/features/quotes/types/quote.types";
import { Alert, Button, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  branchRiskKind,
  CoveragePackage,
  type InsuranceBranch,
} from "@/shared/types/insurance.types";

interface ComparisonStepProps {
  branch: InsuranceBranch;
  /** Seçilen risk objesi (araç veya konut) kimliği; Sağlık için null. */
  riskObjectId: string | null;
  /** Sağlıkta "başkası adına" sigortalı beyanı (ADR-041); kendim için null. */
  insuredPerson: InsuredPersonRequest | null;
  /** Sağlıkta sigara kullanım beyanı (ADR-054); diğer branşlarda gönderilmez. */
  isSmoker: boolean | null;
  onBack: () => void;
  onCreated: (quoteId: string) => void;
}

/**
 * Sihirbaz 3. adım: anlık prim + risk skoru göstergesi ve paket karşılaştırma kartları.
 * Paket seçimi teklifi oluşturur (Priced) ve detay sayfasına yönlendirir.
 */
export function ComparisonStep({ branch, riskObjectId, insuredPerson, isSmoker, onBack, onCreated }: ComparisonStepProps) {
  const kind = branchRiskKind(branch);
  const params: QuoteComparisonParams = {
    branch,
    vehicleId: kind === "vehicle" ? (riskObjectId ?? undefined) : undefined,
    propertyId: kind === "property" ? (riskObjectId ?? undefined) : undefined,
    // "Başkası adına" sağlıkta önizleme primi sigortalının yaşından hesaplanır (ADR-041).
    insuredBirthDate: insuredPerson?.birthDate,
    // ADR-056: Beyan önizlemeye de iletilir → gösterilen prim, oluşturulacak teklifin primiyle aynıdır.
    isSmoker: kind === "none" ? isSmoker : null,
  };

  const comparison = useQuoteComparison(params, true);
  const createQuote = useCreateQuote();
  const [selectingPackage, setSelectingPackage] = useState<CoveragePackage | null>(null);

  const handleSelect = (coveragePackage: CoveragePackage) => {
    setSelectingPackage(coveragePackage);
    createQuote.mutate(
      {
        branch,
        vehicleId: params.vehicleId ?? null,
        propertyId: params.propertyId ?? null,
        coveragePackage,
        insuredPerson,
        // Beyan yalnızca Sağlıkta gönderilir; diğer branşlarda backend bunu reddeder (ADR-054).
        isSmoker: kind === "none" ? isSmoker : null,
      },
      {
        onSuccess: (quote) => onCreated(quote.id),
        onError: () => setSelectingPackage(null),
      },
    );
  };

  if (comparison.isLoading) {
    return (
      <div className="flex flex-col items-center gap-3 py-16">
        <Spinner />
        <p className="text-sm text-muted-foreground">Primler hesaplanıyor…</p>
      </div>
    );
  }

  if (comparison.isError || comparison.data === undefined) {
    return (
      <div className="space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(comparison.error)[0]}</Alert>
        <Button variant="outline" onClick={onBack}>
          Geri
        </Button>
      </div>
    );
  }

  const { productName, riskObject, packages } = comparison.data;
  // Risk skoru pakete göre değişmez (ADR-021); ilk paketten alınır.
  const riskScore = packages[0]?.riskScore;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold">{productName}</h2>
          <p className="text-muted-foreground">{riskObject.display}</p>
        </div>
        {riskScore !== undefined && (
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Risk skoru:</span>
            <RiskScoreBadge score={riskScore} />
          </div>
        )}
      </div>

      {createQuote.isError && (
        <Alert variant="destructive">{getApiErrorMessages(createQuote.error)[0]}</Alert>
      )}

      <div className="grid gap-4 lg:grid-cols-3">
        {packages.map((pkg) => (
          <PackageCard
            key={pkg.coveragePackage}
            pkg={pkg}
            highlighted={pkg.coveragePackage === CoveragePackage.Genisletilmis}
            onSelect={() => handleSelect(pkg.coveragePackage)}
            isSelecting={createQuote.isPending && selectingPackage === pkg.coveragePackage}
            disabled={createQuote.isPending}
          />
        ))}
      </div>

      <Button variant="outline" onClick={onBack} disabled={createQuote.isPending}>
        Geri
      </Button>
    </div>
  );
}

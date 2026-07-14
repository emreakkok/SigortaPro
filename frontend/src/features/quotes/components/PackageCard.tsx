import { CoverageList } from "@/features/quotes/components/CoverageList";
import { PremiumBreakdownList } from "@/features/quotes/components/PremiumBreakdownList";
import type { QuotePackage } from "@/features/quotes/types/quote.types";
import { Button, Card, CardContent, CardHeader, CardTitle, Spinner } from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { COVERAGE_PACKAGE_LABELS } from "@/shared/types/insurance.types";
import { formatCurrency } from "@/shared/utils/format";

interface PackageCardProps {
  pkg: QuotePackage;
  /** Genişletilmiş paketi vurgulamak için (önerilen). */
  highlighted?: boolean;
  onSelect: () => void;
  isSelecting: boolean;
  /** Başka bir paket seçilirken bu kartın butonunu da kilitler. */
  disabled: boolean;
}

/** Karşılaştırmadaki tek teminat paketi kartı: prim, teminatlar, prim dökümü + seçim aksiyonu. */
export function PackageCard({
  pkg,
  highlighted = false,
  onSelect,
  isSelecting,
  disabled,
}: PackageCardProps) {
  return (
    <Card className={cn("flex h-full flex-col", highlighted && "border-primary")}>
      <CardHeader>
        <CardTitle className="flex items-baseline justify-between">
          <span>{COVERAGE_PACKAGE_LABELS[pkg.coveragePackage]}</span>
        </CardTitle>
        <p className="text-2xl font-bold text-primary">{formatCurrency(pkg.totalPremium)}</p>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-4">
        <div>
          <p className="mb-1 text-sm font-semibold">Teminatlar</p>
          <CoverageList coverages={pkg.coverages} />
        </div>
        <details className="text-sm">
          <summary className="cursor-pointer font-semibold text-muted-foreground">
            Prim dökümü
          </summary>
          <div className="mt-2">
            <PremiumBreakdownList items={pkg.premiumBreakdown} />
          </div>
        </details>
        <div className="mt-auto pt-2">
          <Button className="w-full" onClick={onSelect} disabled={disabled}>
            {isSelecting ? (
              <Spinner className="[&>div]:h-4 [&>div]:w-4" />
            ) : (
              "Bu paketle teklif al"
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

import { Card, CardContent } from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import {
  INSURANCE_BRANCH_DESCRIPTIONS,
  INSURANCE_BRANCH_LABELS,
  InsuranceBranch,
} from "@/shared/types/insurance.types";

const BRANCHES: InsuranceBranch[] = [
  InsuranceBranch.Kasko,
  InsuranceBranch.Trafik,
  InsuranceBranch.Konut,
  InsuranceBranch.Dask,
  InsuranceBranch.Saglik,
];

interface BranchStepProps {
  selected: InsuranceBranch | null;
  onSelect: (branch: InsuranceBranch) => void;
}

/** Sihirbaz 1. adım: sigorta branşı seçimi. */
export function BranchStep({ selected, onSelect }: BranchStepProps) {
  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-xl font-semibold">Branş seçin</h2>
        <p className="text-muted-foreground">Hangi sigorta ürünü için teklif almak istiyorsunuz?</p>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {BRANCHES.map((branch) => (
          <button key={branch} type="button" onClick={() => onSelect(branch)} className="text-left">
            <Card
              className={cn(
                "h-full transition-colors hover:border-primary",
                selected === branch && "border-primary ring-2 ring-primary",
              )}
            >
              <CardContent className="py-4">
                <p className="font-semibold">{INSURANCE_BRANCH_LABELS[branch]}</p>
                <p className="text-sm text-muted-foreground">
                  {INSURANCE_BRANCH_DESCRIPTIONS[branch]}
                </p>
              </CardContent>
            </Card>
          </button>
        ))}
      </div>
    </div>
  );
}

import { PolicyDocumentButton } from "@/features/policies/components/PolicyDocumentButton";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import { usePolicy } from "@/features/policies/hooks/usePolicies";
import { CoverageList } from "@/features/quotes/components/CoverageList";
import { Alert, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/**
 * Admin poliçe detay çekmecesi içeriği: künye + risk objesi + teminat tablosu + PDF indirme.
 * `GET /policies/{id}` sahiplik kontrolünde personel muaftır (QuoteAuthorization).
 */
export function AdminPolicyDetailPanel({ policyId }: { policyId: string }) {
  const { data, isLoading, isError, error } = usePolicy(policyId);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || data === undefined) {
    return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="font-mono text-lg font-semibold text-primary">{data.policyNumber}</p>
        <PolicyStatusBadge status={data.status} />
      </div>

      <section className="space-y-1 text-sm">
        <p>
          <span className="text-muted-foreground">Ürün: </span>
          <span className="font-medium">
            {data.productName} ({INSURANCE_BRANCH_LABELS[data.branch]})
          </span>
        </p>
        <p>
          <span className="text-muted-foreground">Paket: </span>
          <span className="font-medium">{COVERAGE_PACKAGE_LABELS[data.coveragePackage]}</span>
        </p>
        <p>
          <span className="text-muted-foreground">Risk objesi: </span>
          <span className="font-medium">{data.riskObject.display}</span>
        </p>
        <p>
          <span className="text-muted-foreground">Dönem: </span>
          <span className="font-medium">
            {formatDate(data.startDate)} — {formatDate(data.endDate)}
          </span>
        </p>
      </section>

      <section className="rounded-lg border bg-muted/40 px-4 py-3">
        <p className="text-sm text-muted-foreground">Toplam Prim</p>
        <p className="text-2xl font-bold tabular-nums">{formatCurrency(data.totalPremium)}</p>
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Teminatlar</h3>
        <CoverageList coverages={data.coverages} />
      </section>

      <PolicyDocumentButton policyId={data.id} policyNumber={data.policyNumber} />
    </div>
  );
}

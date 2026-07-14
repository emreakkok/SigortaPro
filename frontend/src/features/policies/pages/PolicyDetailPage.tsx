import { Link, useParams } from "react-router-dom";
import { PolicyDocumentButton } from "@/features/policies/components/PolicyDocumentButton";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import { usePolicy } from "@/features/policies/hooks/usePolicies";
import { CoverageList } from "@/features/quotes/components/CoverageList";
import {
  Alert,
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/** Poliçe detayı: künye, risk objesi, teminat tablosu ve PDF indirme. */
export default function PolicyDetailPage() {
  const { id = "" } = useParams();
  const { data: policy, isLoading, isError, error } = usePolicy(id);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || policy === undefined) {
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        <Link to="/portal/policies" className="text-sm text-primary hover:underline">
          ← Poliçelerime dön
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link to="/portal/policies" className="text-sm text-primary hover:underline">
        ← Poliçelerime dön
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">{policy.productName}</h1>
            <Badge variant="outline">{INSURANCE_BRANCH_LABELS[policy.branch]}</Badge>
          </div>
          <p className="font-mono text-primary">{policy.policyNumber}</p>
          <p className="text-muted-foreground">{policy.riskObject.display}</p>
        </div>
        <PolicyStatusBadge status={policy.status} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Poliçe Künyesi</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <Row label="Teminat paketi" value={COVERAGE_PACKAGE_LABELS[policy.coveragePackage]} />
          <Row label="Başlangıç" value={formatDate(policy.startDate)} />
          <Row label="Bitiş" value={formatDate(policy.endDate)} />
          <div className="border-t pt-2">
            <Row label="Toplam prim" value={formatCurrency(policy.totalPremium)} strong />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Teminatlar</CardTitle>
        </CardHeader>
        <CardContent>
          <CoverageList coverages={policy.coverages} />
        </CardContent>
      </Card>

      <PolicyDocumentButton policyId={policy.id} policyNumber={policy.policyNumber} variant="default" />
    </div>
  );
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className={strong === true ? "text-base font-bold text-primary" : "font-medium"}>{value}</span>
    </div>
  );
}

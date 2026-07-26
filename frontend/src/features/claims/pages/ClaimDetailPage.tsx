import { Link, useParams } from "react-router-dom";
import { ClaimDocuments } from "@/features/claims/components/ClaimDocuments";
import { ClaimStatusBadge } from "@/features/claims/components/ClaimStatusBadge";
import { ClaimTimeline } from "@/features/claims/components/ClaimTimeline";
import { useClaim } from "@/features/claims/hooks/useClaims";
import {
  Alert,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { formatCurrency, formatDate, formatDateTime } from "@/shared/utils/format";

/** Hasar detayı: künye, tutarlar, değerlendirme notu ve durum zaman çizelgesi. */
export default function ClaimDetailPage() {
  const { id = "" } = useParams();
  const { data: claim, isLoading, isError, error } = useClaim(id);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || claim === undefined) {
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        <Link to="/portal/claims" className="text-sm text-primary hover:underline">
          ← Hasarlarıma dön
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link to="/portal/claims" className="text-sm text-primary hover:underline">
        ← Hasarlarıma dön
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Hasar Detayı</h1>
          <Link
            to={`/portal/policies/${claim.policyId}`}
            className="font-mono text-primary hover:underline"
          >
            {claim.policyNumber}
          </Link>
        </div>
        <ClaimStatusBadge status={claim.status} />
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Bilgiler</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <Row label="Olay zamanı" value={formatDateTime(claim.incidentDate)} />
            <Row label="Bildirim tarihi" value={formatDate(claim.createdAt)} />
            <Row label="Tahmini tutar" value={formatCurrency(claim.estimatedAmount)} />
            {claim.approvedAmount !== null && (
              <Row label="Onaylanan tutar" value={formatCurrency(claim.approvedAmount)} strong />
            )}
            <div className="border-t pt-2">
              <p className="text-muted-foreground">Açıklama</p>
              <p className="mt-1 whitespace-pre-wrap">{claim.description}</p>
            </div>
            {claim.reviewNote !== null && (
              <div className="border-t pt-2">
                <p className="text-muted-foreground">Değerlendirme notu</p>
                <p className="mt-1 whitespace-pre-wrap">{claim.reviewNote}</p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Süreç</CardTitle>
          </CardHeader>
          <CardContent>
            <ClaimTimeline status={claim.status} />
          </CardContent>
        </Card>
      </div>

      {claim.documents.length > 0 && (
        <Card>
          <CardContent className="pt-6">
            <ClaimDocuments claimId={claim.id} documents={claim.documents} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className={strong === true ? "text-base font-bold text-success" : "font-medium"}>{value}</span>
    </div>
  );
}

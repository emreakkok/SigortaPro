import { ClaimDecisionPanel } from "@/features/claims/components/ClaimDecisionPanel";
import { ClaimDocuments } from "@/features/claims/components/ClaimDocuments";
import { ClaimStatusBadge } from "@/features/claims/components/ClaimStatusBadge";
import { ClaimTimeline } from "@/features/claims/components/ClaimTimeline";
import { useClaim } from "@/features/claims/hooks/useClaims";
import { Alert, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { formatCurrency, formatDate, formatDateTime } from "@/shared/utils/format";

/**
 * Admin hasar detay çekmecesi içeriği: künye + açıklama + durum zaman çizelgesi
 * + karar aksiyonları (inceleme/onay/ret/ödeme — durum makinesine göre).
 */
export function AdminClaimDetailPanel({ claimId }: { claimId: string }) {
  const { data, isLoading, isError, error } = useClaim(claimId);

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
        <ClaimStatusBadge status={data.status} />
      </div>

      <section className="space-y-1 text-sm">
        <p>
          <span className="text-muted-foreground">Olay zamanı: </span>
          <span className="font-medium">{formatDateTime(data.incidentDate)}</span>
        </p>
        <p>
          <span className="text-muted-foreground">Bildirim tarihi: </span>
          <span className="font-medium">{formatDate(data.createdAt)}</span>
        </p>
        <p>
          <span className="text-muted-foreground">Tahmini tutar: </span>
          <span className="font-medium">{formatCurrency(data.estimatedAmount)}</span>
        </p>
        {data.approvedAmount !== null && (
          <p>
            <span className="text-muted-foreground">Onaylanan tutar: </span>
            <span className="font-medium text-success">{formatCurrency(data.approvedAmount)}</span>
          </p>
        )}
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Hasar Açıklaması</h3>
        <p className="whitespace-pre-wrap text-sm">{data.description}</p>
      </section>

      {data.reviewNote !== null && (
        <section>
          <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Değerlendirme Notu</h3>
          <p className="whitespace-pre-wrap text-sm">{data.reviewNote}</p>
        </section>
      )}

      {/* Müşterinin yüklediği belgeler (foto/PDF) — hasar değerlendirmesinde acente tarafından görüntülenir. */}
      <ClaimDocuments claimId={data.id} documents={data.documents} />

      <section>
        <h3 className="mb-2 text-sm font-semibold text-muted-foreground">Süreç</h3>
        <ClaimTimeline status={data.status} />
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-muted-foreground">Karar</h3>
        <ClaimDecisionPanel claim={data} />
      </section>
    </div>
  );
}

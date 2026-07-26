import { Link } from "react-router-dom";
import { useAcceptRenewal } from "@/features/renewals/hooks/useRenewals";
import { QuoteValidity } from "@/features/quotes/components/QuoteValidity";
import type { Renewal } from "@/features/renewals/types/renewal.types";
import {
  Alert,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { INSURANCE_BRANCH_LABELS, QuoteStatus } from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/**
 * Tek bir yenileme teklifi kartı: künye + prim + durum bazlı aksiyon.
 * Aksiyonlar, yenilemenin `isAccepted` bayrağına DEĞİL, yeni dönem teklifinin GERÇEK durumuna
 * (`newQuoteStatus`) göre belirlenir — çünkü ödeme uygunluğunun tek doğru sinyali teklif durumudur.
 * Böylece teklif hangi yoldan onaylanırsa onaylansın (bu kart ya da teklif detay ekranı) doğru aksiyon
 * gösterilir ve müşteri ödeme aşamasına tutarlı biçimde ilerleyebilir.
 */
export function RenewalCard({ renewal }: { renewal: Renewal }) {
  const accept = useAcceptRenewal();
  const status = renewal.newQuoteStatus;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <span className="font-mono text-primary">{renewal.policyNumber}</span>
            <Badge variant="outline">{INSURANCE_BRANCH_LABELS[renewal.branch]}</Badge>
          </span>
          {status === QuoteStatus.Approved && <Badge variant="success">Onaylandı</Badge>}
          {status === QuoteStatus.Purchased && <Badge variant="success">Poliçeleştirildi</Badge>}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-baseline justify-between">
          <span className="text-muted-foreground">Yenileme primi</span>
          <span className="text-xl font-bold text-primary">{formatCurrency(renewal.offeredPremium)}</span>
        </div>
        {/* Geçerlilik/son kabul tarihi yalnızca AÇIK teklif (Priced) için anlamlıdır; onaylanmış/
            poliçeleştirilmiş yenilemede "süresi doldu" göstermek çelişkili olurdu. */}
        <p className="text-sm text-muted-foreground">
          Sunulma {formatDate(renewal.offeredAt)}
          {status === QuoteStatus.Priced && (
            <>
              {" · "}
              <QuoteValidity validUntil={renewal.validUntil} />
            </>
          )}
        </p>

        {accept.isError && (
          <Alert variant="destructive">{getApiErrorMessages(accept.error)[0]}</Alert>
        )}

        {/* Priced → müşteri yenilemeyi onaylar (teklif Approved olur, ödemeye hazırlanır). */}
        {status === QuoteStatus.Priced && (
          <div className="flex items-center gap-3">
            <Button onClick={() => accept.mutate(renewal.id)} disabled={accept.isPending}>
              {accept.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Yenilemeyi Onayla"}
            </Button>
            <Link
              to={`/portal/quotes/${renewal.newQuoteId}`}
              className="text-sm text-primary hover:underline"
            >
              Teklifi görüntüle
            </Link>
          </div>
        )}

        {/* Approved → ödeme aşamasına geçilebilir (tekrar onaylama sunulmaz). */}
        {status === QuoteStatus.Approved && (
          <div className="flex items-center gap-3">
            <Link to={`/portal/quotes/${renewal.newQuoteId}/purchase`}>
              <Button>Ödemeye Geç</Button>
            </Link>
            <Link
              to={`/portal/quotes/${renewal.newQuoteId}`}
              className="text-sm text-primary hover:underline"
            >
              Teklifi görüntüle
            </Link>
          </div>
        )}

        {/* Purchased → yeni dönem poliçesi oluşturuldu. */}
        {status === QuoteStatus.Purchased && (
          <div className="flex items-center gap-3">
            <Link to="/portal/policies">
              <Button variant="outline">Poliçelerim</Button>
            </Link>
          </div>
        )}

        {/* Rejected / Expired → aksiyon yok. */}
        {(status === QuoteStatus.Rejected || status === QuoteStatus.Expired) && (
          <p className="text-sm text-muted-foreground">
            Bu yenileme teklifi artık geçerli değil.
          </p>
        )}
      </CardContent>
    </Card>
  );
}

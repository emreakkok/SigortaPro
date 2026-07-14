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
import { INSURANCE_BRANCH_LABELS } from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/** Tek bir yenileme teklifi kartı: künye + prim + onay / ödemeye geç akışı. */
export function RenewalCard({ renewal }: { renewal: Renewal }) {
  const accept = useAcceptRenewal();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center justify-between gap-2 text-base">
          <span className="flex items-center gap-2">
            <span className="font-mono text-primary">{renewal.policyNumber}</span>
            <Badge variant="outline">{INSURANCE_BRANCH_LABELS[renewal.branch]}</Badge>
          </span>
          {renewal.isAccepted && <Badge variant="success">Onaylandı</Badge>}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-baseline justify-between">
          <span className="text-muted-foreground">Yenileme primi</span>
          <span className="text-xl font-bold text-primary">{formatCurrency(renewal.offeredPremium)}</span>
        </div>
        <p className="text-sm text-muted-foreground">
          Sunulma {formatDate(renewal.offeredAt)} · <QuoteValidity validUntil={renewal.validUntil} />
        </p>

        {accept.isError && (
          <Alert variant="destructive">{getApiErrorMessages(accept.error)[0]}</Alert>
        )}

        {renewal.isAccepted ? (
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
        ) : (
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
      </CardContent>
    </Card>
  );
}

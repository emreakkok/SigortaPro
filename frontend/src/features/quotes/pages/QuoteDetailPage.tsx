import { Link, useParams } from "react-router-dom";
import { CoverageList } from "@/features/quotes/components/CoverageList";
import { PremiumBreakdownList } from "@/features/quotes/components/PremiumBreakdownList";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { QuoteValidity } from "@/features/quotes/components/QuoteValidity";
import { RiskScoreBadge } from "@/features/quotes/components/RiskScoreBadge";
import { useApproveQuote, useQuote, useRejectQuote } from "@/features/quotes/hooks/useQuotes";
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
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
  QuoteStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/** Teklif detayı: prim dökümü, teminatlar ve durum aksiyonları (onayla/reddet). */
export default function QuoteDetailPage() {
  const { id = "" } = useParams();
  const { data: quote, isLoading, isError, error } = useQuote(id);
  const approve = useApproveQuote();
  const reject = useRejectQuote();

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || quote === undefined) {
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        <Link to="/portal/quotes" className="text-sm text-primary hover:underline">
          ← Tekliflerime dön
        </Link>
      </div>
    );
  }

  const isPriced = quote.status === QuoteStatus.Priced;
  const isApproved = quote.status === QuoteStatus.Approved;
  const actionError = approve.isError
    ? approve.error
    : reject.isError
      ? reject.error
      : undefined;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link to="/portal/quotes" className="text-sm text-primary hover:underline">
        ← Tekliflerime dön
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-2xl font-bold tracking-tight">{quote.productName}</h1>
            <Badge variant="outline">{INSURANCE_BRANCH_LABELS[quote.branch]}</Badge>
          </div>
          <p className="text-muted-foreground">{quote.riskObject.display}</p>
        </div>
        <QuoteStatusBadge status={quote.status} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Prim</span>
            <RiskScoreBadge score={quote.riskScore} />
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          <div className="flex items-baseline justify-between">
            <span className="text-muted-foreground">Teminat paketi</span>
            <span className="font-medium">{COVERAGE_PACKAGE_LABELS[quote.coveragePackage]}</span>
          </div>
          <div className="flex items-baseline justify-between">
            <span className="text-muted-foreground">Baz prim</span>
            <span>{formatCurrency(quote.basePremium)}</span>
          </div>
          <div className="flex items-baseline justify-between border-t pt-2">
            <span className="font-semibold">Toplam prim</span>
            <span className="text-xl font-bold text-primary">
              {formatCurrency(quote.totalPremium)}
            </span>
          </div>
          <p className="text-sm text-muted-foreground">
            <QuoteValidity validUntil={quote.validUntil} /> · Oluşturma {formatDate(quote.createdAt)}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Teminatlar</CardTitle>
        </CardHeader>
        <CardContent>
          <CoverageList coverages={quote.coverages} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Prim Dökümü</CardTitle>
        </CardHeader>
        <CardContent>
          <PremiumBreakdownList items={quote.premiumBreakdown} />
        </CardContent>
      </Card>

      {actionError !== undefined && (
        <Alert variant="destructive">{getApiErrorMessages(actionError)[0]}</Alert>
      )}

      {isApproved && (
        <Alert variant="success">
          Teklifiniz onaylandı. Ödemeyi tamamlayarak poliçenizi oluşturabilirsiniz.
        </Alert>
      )}

      {(isPriced || isApproved) && (
        <div className="flex gap-3">
          {isPriced && (
            <Button
              onClick={() => approve.mutate(quote.id)}
              disabled={approve.isPending || reject.isPending}
            >
              {approve.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Onayla"}
            </Button>
          )}
          {isApproved && (
            <Link to={`/portal/quotes/${quote.id}/purchase`}>
              <Button>Satın Al</Button>
            </Link>
          )}
          <Button
            variant="outline"
            onClick={() => reject.mutate(quote.id)}
            disabled={approve.isPending || reject.isPending}
          >
            {reject.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Reddet"}
          </Button>
        </div>
      )}
    </div>
  );
}

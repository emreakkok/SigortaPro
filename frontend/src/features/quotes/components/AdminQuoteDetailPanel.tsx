import { CoverageList } from "@/features/quotes/components/CoverageList";
import { PremiumBreakdownList } from "@/features/quotes/components/PremiumBreakdownList";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { QuoteValidity } from "@/features/quotes/components/QuoteValidity";
import { RiskScoreBadge } from "@/features/quotes/components/RiskScoreBadge";
import { useQuote } from "@/features/quotes/hooks/useQuotes";
import { Alert, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/**
 * Admin teklif detay çekmecesi içeriği: künye + risk objesi + prim dökümü + teminatlar.
 * Salt okunurdur — teklif durum geçişleri (onay/ret/satın alma) müşteri akışıdır.
 */
export function AdminQuoteDetailPanel({ quoteId }: { quoteId: string }) {
  const { data, isLoading, isError, error } = useQuote(quoteId);

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
      <div className="flex flex-wrap items-center gap-2">
        <QuoteStatusBadge status={data.status} />
        <RiskScoreBadge score={data.riskScore} />
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
          <span className="text-muted-foreground">Oluşturulma: </span>
          <span className="font-medium">{formatDate(data.createdAt)}</span>
        </p>
        <p className="text-muted-foreground">
          <QuoteValidity validUntil={data.validUntil} />
        </p>
      </section>

      <section className="rounded-lg border bg-muted/40 px-4 py-3">
        <p className="text-sm text-muted-foreground">Toplam Prim</p>
        <p className="text-2xl font-bold tabular-nums">{formatCurrency(data.totalPremium)}</p>
        <p className="text-xs text-muted-foreground">
          Baz prim {formatCurrency(data.basePremium)}
        </p>
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Prim Dökümü</h3>
        <PremiumBreakdownList items={data.premiumBreakdown} />
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Teminatlar</h3>
        <CoverageList coverages={data.coverages} />
      </section>
    </div>
  );
}

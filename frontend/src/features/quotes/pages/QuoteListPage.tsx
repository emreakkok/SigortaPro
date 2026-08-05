import { useState } from "react";
import { Link } from "react-router-dom";
import { QuoteSourceBadge } from "@/features/quotes/components/QuoteSourceBadge";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { QuoteValidity } from "@/features/quotes/components/QuoteValidity";
import { useQuoteList } from "@/features/quotes/hooks/useQuotes";
import {
  Alert,
  Badge,
  Button,
  Card,
  CardContent,
  EmptyState,
  FileTextIcon,
  Label,
  Pagination,
  Select,
  SkeletonRows,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { PORTAL_PAGE_SIZE } from "@/shared/lib/pagination";
import {
  INSURANCE_BRANCH_LABELS,
  QUOTE_STATUS_LABELS,
  QuoteStatus,
  type InsuranceBranch,
} from "@/shared/types/insurance.types";
import { formatCurrency } from "@/shared/utils/format";

const STATUS_FILTER_OPTIONS = Object.values(QuoteStatus);

/** Tekliflerim listesi: durum rozetleri, geçerlilik sayacı, durum filtresi ve sayfalama. */
export default function QuoteListPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<QuoteStatus | undefined>(undefined);

  const { data, isLoading, isError, error, isFetching } = useQuoteList({
    page,
    pageSize: PORTAL_PAGE_SIZE,
    status,
  });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Tekliflerim</h1>
          <p className="text-muted-foreground">Aldığınız teklifleri görüntüleyin ve yönetin.</p>
        </div>
        <Link to="/portal/quotes/new">
          <Button>Yeni Teklif Al</Button>
        </Link>
      </div>

      <div className="flex items-end gap-3">
        <div className="space-y-2">
          <Label htmlFor="statusFilter">Durum</Label>
          <Select
            id="statusFilter"
            value={status ?? ""}
            onChange={(event) => {
              const value = event.target.value;
              setStatus(value === "" ? undefined : (Number(value) as QuoteStatus));
              setPage(1);
            }}
          >
            <option value="">Tümü</option>
            {STATUS_FILTER_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {QUOTE_STATUS_LABELS[value]}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {isLoading ? (
        <SkeletonRows rows={4} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<FileTextIcon />}
            title="Henüz teklifiniz yok"
            description="Aracınız, konutunuz veya sağlığınız için dakikalar içinde teklif alın; üç teminat paketini ücretsiz karşılaştırın."
            action={
              <Link to="/portal/quotes/new">
                <Button>İlk Teklifinizi Alın</Button>
              </Link>
            }
          />
        </Card>
      ) : (
        <>
          <div className={isFetching ? "space-y-3 opacity-60" : "space-y-3"}>
            {data.items.map((quote) => (
              <Link key={quote.id} to={`/portal/quotes/${quote.id}`} className="block">
                <Card className="transition-colors hover:border-primary">
                  <CardContent className="flex items-center justify-between gap-4 py-4">
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="truncate font-medium">{quote.productName}</p>
                        <Badge variant="outline">{INSURANCE_BRANCH_LABELS[quote.branch as InsuranceBranch]}</Badge>
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {formatCurrency(quote.totalPremium)}
                      </p>
                      <p className="mt-1 text-xs">
                        <QuoteValidity validUntil={quote.validUntil} />
                      </p>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      <QuoteStatusBadge status={quote.status} />
                      {/* Acente tarafından oluşturulduysa görünür; kendi oluşturduğu tekliflerde gizli (hideSelf). */}
                      <QuoteSourceBadge source={quote.source} audience="customer" hideSelf />
                    </div>
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            onPageChange={setPage}
            totalCount={data.totalCount}
          />
        </>
      )}
    </div>
  );
}

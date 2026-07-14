import { useState } from "react";
import { AdminQuoteDetailPanel } from "@/features/quotes/components/AdminQuoteDetailPanel";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { useQuoteList } from "@/features/quotes/hooks/useQuotes";
import {
  Alert,
  Card,
  CardContent,
  Drawer,
  Label,
  Pagination,
  Select,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
  InsuranceBranch,
  QUOTE_STATUS_LABELS,
  QuoteStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

const PAGE_SIZE = 20;
const STATUS_OPTIONS = Object.values(QuoteStatus);
const BRANCH_OPTIONS = Object.values(InsuranceBranch);

/** Teklif yönetimi: durum + branş filtresi, tüm müşterilerin teklifleri, detay çekmecesi. */
export default function AdminQuoteListPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<QuoteStatus | undefined>(undefined);
  const [branch, setBranch] = useState<InsuranceBranch | undefined>(undefined);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const { data, isLoading, isError, error, isFetching } = useQuoteList({
    page,
    pageSize: PAGE_SIZE,
    status,
    branch,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Teklifler</h1>
        <p className="text-muted-foreground">Tüm müşterilerin teklifleri ve fiyatlama detayları.</p>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-48 space-y-2">
          <Label htmlFor="quoteStatusFilter">Durum</Label>
          <Select
            id="quoteStatusFilter"
            value={status ?? ""}
            onChange={(event) => {
              const value = event.target.value;
              setStatus(value === "" ? undefined : (Number(value) as QuoteStatus));
              setPage(1);
            }}
          >
            <option value="">Tümü</option>
            {STATUS_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {QUOTE_STATUS_LABELS[value]}
              </option>
            ))}
          </Select>
        </div>
        <div className="w-48 space-y-2">
          <Label htmlFor="quoteBranchFilter">Branş</Label>
          <Select
            id="quoteBranchFilter"
            value={branch ?? ""}
            onChange={(event) => {
              const value = event.target.value;
              setBranch(value === "" ? undefined : (Number(value) as InsuranceBranch));
              setPage(1);
            }}
          >
            <option value="">Tümü</option>
            {BRANCH_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {INSURANCE_BRANCH_LABELS[value]}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            Filtrelerle eşleşen teklif bulunamadı.
          </CardContent>
        </Card>
      ) : (
        <>
          <Card className={isFetching ? "opacity-60" : undefined}>
            <CardContent className="overflow-x-auto p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-muted-foreground">
                    <th className="px-4 py-3 font-medium">Tarih</th>
                    <th className="px-4 py-3 font-medium">Ürün</th>
                    <th className="px-4 py-3 font-medium">Paket</th>
                    <th className="px-4 py-3 font-medium">Prim</th>
                    <th className="px-4 py-3 font-medium">Geçerlilik</th>
                    <th className="px-4 py-3 font-medium">Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((quote) => (
                    <tr
                      key={quote.id}
                      onClick={() => setSelectedId(quote.id)}
                      className="cursor-pointer border-b last:border-0 transition-colors hover:bg-accent/50"
                    >
                      <td className="px-4 py-3">{formatDate(quote.createdAt)}</td>
                      <td className="px-4 py-3 font-medium">{quote.productName}</td>
                      <td className="px-4 py-3">{COVERAGE_PACKAGE_LABELS[quote.coveragePackage]}</td>
                      <td className="px-4 py-3 tabular-nums">{formatCurrency(quote.totalPremium)}</td>
                      <td className="px-4 py-3">
                        {quote.validUntil === null ? "—" : formatDate(quote.validUntil)}
                      </td>
                      <td className="px-4 py-3">
                        <QuoteStatusBadge status={quote.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
          <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />
        </>
      )}

      <Drawer
        open={selectedId !== null}
        onClose={() => setSelectedId(null)}
        title="Teklif Detayı"
        description="Prim dökümü ve teminatlar"
      >
        {selectedId !== null && <AdminQuoteDetailPanel quoteId={selectedId} />}
      </Drawer>
    </div>
  );
}

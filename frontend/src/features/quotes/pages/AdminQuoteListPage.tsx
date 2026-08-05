import { useState } from "react";
import { AdminQuoteDetailPanel } from "@/features/quotes/components/AdminQuoteDetailPanel";
import { QuoteSourceBadge } from "@/features/quotes/components/QuoteSourceBadge";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { useQuoteList } from "@/features/quotes/hooks/useQuotes";
import {
  Alert,
  Card,
  CardContent,
  Drawer,
  EmptyState,
  FileTextIcon,
  Input,
  Label,
  PageSizeSelector,
  Pagination,
  Select,
  SkeletonRows,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { useFocusedRecord } from "@/shared/hooks/useFocusedRecord";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
  InsuranceBranch,
  QUOTE_STATUS_LABELS,
  QuoteStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate, formatPhoneNumber } from "@/shared/utils/format";

const STATUS_OPTIONS = Object.values(QuoteStatus);
const BRANCH_OPTIONS = Object.values(InsuranceBranch);

/** Teklif yönetimi: durum + branş filtresi, tüm müşterilerin teklifleri, detay çekmecesi. */
export default function AdminQuoteListPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [status, setStatus] = useState<QuoteStatus | undefined>(undefined);
  const [branch, setBranch] = useState<InsuranceBranch | undefined>(undefined);
  const [createdByMe, setCreatedByMe] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  // ADR-047: bildirimden `?focus=<id>` ile gelindiğinde ilgili teklifin çekmecesi doğrudan açılır.
  const [selectedId, setSelectedId] = useFocusedRecord();

  const debouncedSearch = useDebounce(searchTerm);

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const { data, isLoading, isError, error, isFetching } = useQuoteList({
    page,
    pageSize,
    status,
    branch,
    search: debouncedSearch === "" ? undefined : debouncedSearch,
    createdByMe: createdByMe ? true : undefined,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Teklifler</h1>
        <p className="text-muted-foreground">Tüm müşterilerin teklifleri ve fiyatlama detayları.</p>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64 space-y-2">
          <Label htmlFor="quoteCustomerSearch">Müşteri Ara</Label>
          <Input
            id="quoteCustomerSearch"
            placeholder="Ad, soyad veya telefon"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPage(1);
            }}
          />
        </div>
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
        {/* Acente destekli: personelin yalnızca kendi müşteri adına oluşturduğu teklifleri süzmesi için. */}
        <label className="flex h-10 cursor-pointer items-center gap-2 text-sm">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-border accent-primary"
            checked={createdByMe}
            onChange={(event) => {
              setCreatedByMe(event.target.checked);
              setPage(1);
            }}
          />
          Yalnızca benim oluşturduklarım
        </label>
      </div>

      {isLoading ? (
        <SkeletonRows rows={6} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<FileTextIcon />}
            title="Teklif bulunamadı"
            description="Filtrelerle eşleşen teklif yok. Durum filtresini değiştirmeyi deneyin."
          />
        </Card>
      ) : (
        <>
          <Card className={isFetching ? "opacity-60" : undefined}>
            <CardContent className="overflow-x-auto p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-muted-foreground">
                    <th className="px-4 py-3 font-medium">Müşteri</th>
                    <th className="px-4 py-3 font-medium">Kaynak</th>
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
                      <td className="px-4 py-3">
                        <div className="font-medium">{quote.customerFullName || "—"}</div>
                        {quote.customerPhone !== null && (
                          <div className="text-xs text-muted-foreground tabular-nums">
                            {formatPhoneNumber(quote.customerPhone)}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <QuoteSourceBadge source={quote.source} audience="staff" />
                      </td>
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
          <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} totalCount={data.totalCount}>
            <PageSizeSelector value={pageSize} onChange={handlePageSizeChange} />
          </Pagination>
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

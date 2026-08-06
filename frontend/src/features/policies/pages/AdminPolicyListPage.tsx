import { useState } from "react";
import { usePolicyReport } from "@/features/dashboard/hooks/useDashboard";
import { AdminPolicyDetailPanel } from "@/features/policies/components/AdminPolicyDetailPanel";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import {
  Alert,
  Card,
  CardContent,
  Drawer,
  Input,
  EmptyState,
  Label,
  PageSizeSelector,
  Pagination,
  ShieldCheckIcon,
  SkeletonRows,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { useFocusedRecord } from "@/shared/hooks/useFocusedRecord";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";
import { INSURANCE_BRANCH_LABELS } from "@/shared/types/insurance.types";
import { formatCurrency, formatDate, formatPhoneNumber } from "@/shared/utils/format";

/** `<input type="date">` değeri için yyyy-MM-dd biçimi (yerel saat). */
function toDateInputValue(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

/** Varsayılan rapor aralığı: son 12 ay → 30 gün sonrası (yenileme poliçeleri ileri başlayabilir). */
function defaultRange(): { from: string; to: string } {
  const now = new Date();
  const from = new Date(now);
  from.setFullYear(from.getFullYear() - 1);
  const to = new Date(now);
  to.setDate(to.getDate() + 30);
  return { from: toDateInputValue(from), to: toDateInputValue(to) };
}

/**
 * Poliçe yönetimi: tarih aralıklı poliçe raporu (`GET /dashboard/reports/policies`,
 * poliçe başlangıç tarihine göre) + detay çekmecesi. Personelin tüm poliçeleri
 * listeleyebildiği tek uç budur — `GET /policies` müşteri kapsamlıdır.
 */
export default function AdminPolicyListPage() {
  const [range, setRange] = useState(defaultRange);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [searchTerm, setSearchTerm] = useState("");
  // bildirimden `?focus=<id>` ile gelindiğinde ilgili poliçenin çekmecesi doğrudan açılır.
  const [selectedId, setSelectedId] = useFocusedRecord();

  const debouncedSearch = useDebounce(searchTerm);
  const rangeValid = range.from !== "" && range.to !== "" && range.from <= range.to;

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const { data, isLoading, isError, error, isFetching } = usePolicyReport(
    {
      from: range.from,
      to: range.to,
      page,
      pageSize,
      search: debouncedSearch === "" ? undefined : debouncedSearch,
    },
    rangeValid,
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Poliçeler</h1>
        <p className="text-muted-foreground">
          Başlangıç tarihi aralığına göre tüm müşterilerin poliçeleri.
        </p>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64 space-y-2">
          <Label htmlFor="policyCustomerSearch">Müşteri Ara</Label>
          <Input
            id="policyCustomerSearch"
            placeholder="Ad, soyad, telefon veya poliçe no"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="w-44 space-y-2">
          <Label htmlFor="policyFrom">Başlangıç (from)</Label>
          <Input
            id="policyFrom"
            type="date"
            value={range.from}
            onChange={(event) => {
              setRange((current) => ({ ...current, from: event.target.value }));
              setPage(1);
            }}
          />
        </div>
        <div className="w-44 space-y-2">
          <Label htmlFor="policyTo">Bitiş (to)</Label>
          <Input
            id="policyTo"
            type="date"
            value={range.to}
            onChange={(event) => {
              setRange((current) => ({ ...current, to: event.target.value }));
              setPage(1);
            }}
          />
        </div>
      </div>

      {!rangeValid ? (
        <Alert variant="destructive">Bitiş tarihi başlangıç tarihinden önce olamaz.</Alert>
      ) : isLoading ? (
        <SkeletonRows rows={6} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ShieldCheckIcon />}
            title="Poliçe bulunamadı"
            description="Seçilen tarih aralığında poliçe yok. Tarih aralığını genişletmeyi deneyin."
          />
        </Card>
      ) : (
        <>
          <Card className={isFetching ? "opacity-60" : undefined}>
            <CardContent className="overflow-x-auto p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-muted-foreground">
                    <th className="px-4 py-3 font-medium">Poliçe No</th>
                    <th className="px-4 py-3 font-medium">Müşteri</th>
                    <th className="px-4 py-3 font-medium">Branş</th>
                    <th className="px-4 py-3 font-medium">Dönem</th>
                    <th className="px-4 py-3 font-medium">Prim</th>
                    <th className="px-4 py-3 font-medium">Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((policy) => (
                    <tr
                      key={policy.id}
                      onClick={() => setSelectedId(policy.id)}
                      className="cursor-pointer border-b last:border-0 transition-colors hover:bg-accent/50"
                    >
                      <td className="px-4 py-3 font-mono text-primary">{policy.policyNumber}</td>
                      <td className="px-4 py-3">
                        <div className="font-medium">{policy.customerFullName || "—"}</div>
                        {policy.customerPhone !== null && policy.customerPhone !== undefined && (
                          <div className="text-xs text-muted-foreground tabular-nums">
                            {formatPhoneNumber(policy.customerPhone)}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3">{INSURANCE_BRANCH_LABELS[policy.branch]}</td>
                      <td className="px-4 py-3">
                        {formatDate(policy.startDate)} — {formatDate(policy.endDate)}
                      </td>
                      <td className="px-4 py-3 tabular-nums">{formatCurrency(policy.totalPremium)}</td>
                      <td className="px-4 py-3">
                        <PolicyStatusBadge status={policy.status} />
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
        title="Poliçe Detayı"
        description="Künye, teminatlar ve PDF"
      >
        {selectedId !== null && <AdminPolicyDetailPanel policyId={selectedId} />}
      </Drawer>
    </div>
  );
}

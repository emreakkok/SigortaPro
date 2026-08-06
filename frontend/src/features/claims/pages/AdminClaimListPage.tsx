import { useState } from "react";
import { AdminClaimDetailPanel } from "@/features/claims/components/AdminClaimDetailPanel";
import { ClaimStatusBadge } from "@/features/claims/components/ClaimStatusBadge";
import { useClaimList } from "@/features/claims/hooks/useClaims";
import {
  Alert,
  Card,
  CardContent,
  Drawer,
  Label,
  AlertTriangleIcon,
  EmptyState,
  PageSizeSelector,
  Pagination,
  Select,
  SkeletonRows,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useFocusedRecord } from "@/shared/hooks/useFocusedRecord";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";
import {
  CLAIM_STATUS_LABELS,
  ClaimStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

const STATUS_OPTIONS = Object.values(ClaimStatus);

/** Hasar yönetimi: durum filtresi, tüm müşterilerin hasarları, detay çekmecesi + karar aksiyonları. */
export default function AdminClaimListPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [status, setStatus] = useState<ClaimStatus | undefined>(undefined);
  // bildirimden `?focus=<id>` ile gelindiğinde ilgili hasar dosyasının çekmecesi doğrudan açılır.
  const [selectedId, setSelectedId] = useFocusedRecord();

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const { data, isLoading, isError, error, isFetching } = useClaimList({
    page,
    pageSize,
    status,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Hasarlar</h1>
        <p className="text-muted-foreground">
          Bildirilen hasarlar; inceleme, onay/ret ve ödeme aksiyonları.
        </p>
      </div>

      <div className="flex items-end gap-3">
        <div className="w-48 space-y-2">
          <Label htmlFor="claimStatusFilter">Durum</Label>
          <Select
            id="claimStatusFilter"
            value={status ?? ""}
            onChange={(event) => {
              const value = event.target.value;
              setStatus(value === "" ? undefined : (Number(value) as ClaimStatus));
              setPage(1);
            }}
          >
            <option value="">Tümü</option>
            {STATUS_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {CLAIM_STATUS_LABELS[value]}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {isLoading ? (
        <SkeletonRows rows={6} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<AlertTriangleIcon />}
            title="Hasar bulunamadı"
            description="Filtrelerle eşleşen hasar kaydı yok. Durum filtresini değiştirmeyi deneyin."
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
                    <th className="px-4 py-3 font-medium">Olay Tarihi</th>
                    <th className="px-4 py-3 font-medium">Bildirim</th>
                    <th className="px-4 py-3 font-medium">Tahmini</th>
                    <th className="px-4 py-3 font-medium">Onaylanan</th>
                    <th className="px-4 py-3 font-medium">Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((claim) => (
                    <tr
                      key={claim.id}
                      onClick={() => setSelectedId(claim.id)}
                      className="cursor-pointer border-b last:border-0 transition-colors hover:bg-accent/50"
                    >
                      <td className="px-4 py-3 font-mono text-primary">{claim.policyNumber}</td>
                      <td className="px-4 py-3">{formatDate(claim.incidentDate)}</td>
                      <td className="px-4 py-3">{formatDate(claim.createdAt)}</td>
                      <td className="px-4 py-3 tabular-nums">
                        {formatCurrency(claim.estimatedAmount)}
                      </td>
                      <td className="px-4 py-3 tabular-nums">
                        {claim.approvedAmount === null ? "—" : formatCurrency(claim.approvedAmount)}
                      </td>
                      <td className="px-4 py-3">
                        <ClaimStatusBadge status={claim.status} />
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
        title="Hasar Detayı"
        description="Süreç ve karar aksiyonları"
      >
        {selectedId !== null && <AdminClaimDetailPanel claimId={selectedId} />}
      </Drawer>
    </div>
  );
}

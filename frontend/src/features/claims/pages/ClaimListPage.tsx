import { useState } from "react";
import { Link } from "react-router-dom";
import { ClaimStatusBadge } from "@/features/claims/components/ClaimStatusBadge";
import { useClaimList } from "@/features/claims/hooks/useClaims";
import { Alert, Button, Card, CardContent, Label, Select, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  CLAIM_STATUS_LABELS,
  ClaimStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

const STATUS_FILTER_OPTIONS = Object.values(ClaimStatus);

/** Hasarlarım: durum filtresi, sayfalama ve hasar kartları (durum rozeti + tutar). */
export default function ClaimListPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ClaimStatus | undefined>(undefined);

  const { data, isLoading, isError, error, isFetching } = useClaimList({
    page,
    pageSize: 10,
    status,
  });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Hasarlarım</h1>
          <p className="text-muted-foreground">Hasar bildirimlerinizi ve süreçlerini takip edin.</p>
        </div>
        <Link to="/portal/claims/new">
          <Button>Hasar Bildir</Button>
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
              setStatus(value === "" ? undefined : (Number(value) as ClaimStatus));
              setPage(1);
            }}
          >
            <option value="">Tümü</option>
            {STATUS_FILTER_OPTIONS.map((value) => (
              <option key={value} value={value}>
                {CLAIM_STATUS_LABELS[value]}
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
            Henüz hasar bildiriminiz yok.{" "}
            <Link to="/portal/claims/new" className="font-medium text-primary hover:underline">
              Hasar bildirin.
            </Link>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className={isFetching ? "space-y-3 opacity-60" : "space-y-3"}>
            {data.items.map((claim) => (
              <Link key={claim.id} to={`/portal/claims/${claim.id}`} className="block">
                <Card className="transition-colors hover:border-primary">
                  <CardContent className="flex items-center justify-between gap-4 py-4">
                    <div className="min-w-0">
                      <p className="truncate font-mono text-sm text-primary">{claim.policyNumber}</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Olay {formatDate(claim.incidentDate)} · Tahmini {formatCurrency(claim.estimatedAmount)}
                      </p>
                      {claim.approvedAmount !== null && (
                        <p className="mt-1 text-sm text-success">
                          Onaylanan {formatCurrency(claim.approvedAmount)}
                        </p>
                      )}
                    </div>
                    <ClaimStatusBadge status={claim.status} />
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>

          <div className="flex items-center justify-between">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Önceki
            </Button>
            <span className="text-sm text-muted-foreground">
              Sayfa {data.page} / {data.totalPages === 0 ? 1 : data.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Sonraki
            </Button>
          </div>
        </>
      )}
    </div>
  );
}

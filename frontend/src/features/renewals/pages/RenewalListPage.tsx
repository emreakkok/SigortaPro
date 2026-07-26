import { useState } from "react";
import { Link } from "react-router-dom";
import { RenewalCard } from "@/features/renewals/components/RenewalCard";
import { useRenewalList } from "@/features/renewals/hooks/useRenewals";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  Pagination,
  RefreshIcon,
  SkeletonRows,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { PORTAL_PAGE_SIZE } from "@/shared/lib/pagination";

/** Yenilemeler: süresi yaklaşan poliçeler için otomatik üretilen yenileme teklifleri ve onay akışı. */
export default function RenewalListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, error, isFetching } = useRenewalList({ page, pageSize: PORTAL_PAGE_SIZE });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Yenilemeler</h1>
        <p className="text-muted-foreground">
          Süresi yaklaşan poliçeleriniz için hazırlanan yenileme tekliflerini onaylayın.
        </p>
      </div>

      {isLoading ? (
        <SkeletonRows rows={3} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<RefreshIcon />}
            title="Bekleyen yenileme yok"
            description="Yenileme teklifleri, poliçenizin bitişine 30 gün kaldığında otomatik hazırlanır ve burada görünür."
            action={
              <Link to="/portal/policies">
                <Button variant="outline">Poliçelerim</Button>
              </Link>
            }
          />
        </Card>
      ) : (
        <>
          <div className={isFetching ? "space-y-4 opacity-60" : "space-y-4"}>
            {data.items.map((renewal) => (
              <RenewalCard key={renewal.id} renewal={renewal} />
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

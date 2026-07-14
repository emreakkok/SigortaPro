import { useState } from "react";
import { Link } from "react-router-dom";
import { RenewalCard } from "@/features/renewals/components/RenewalCard";
import { useRenewalList } from "@/features/renewals/hooks/useRenewals";
import { Alert, Button, Card, CardContent, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/** Yenilemeler: süresi yaklaşan poliçeler için otomatik üretilen yenileme teklifleri ve onay akışı. */
export default function RenewalListPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, error, isFetching } = useRenewalList({ page, pageSize: 10 });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Yenilemeler</h1>
        <p className="text-muted-foreground">
          Süresi yaklaşan poliçeleriniz için hazırlanan yenileme tekliflerini onaylayın.
        </p>
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
            Şu anda bekleyen yenileme teklifiniz yok. Yenileme teklifleri, poliçenizin bitişine 30 gün
            kaladığında otomatik olarak hazırlanır ve burada görünür.{" "}
            <Link to="/portal/policies" className="font-medium text-primary hover:underline">
              Poliçelerim
            </Link>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className={isFetching ? "space-y-4 opacity-60" : "space-y-4"}>
            {data.items.map((renewal) => (
              <RenewalCard key={renewal.id} renewal={renewal} />
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

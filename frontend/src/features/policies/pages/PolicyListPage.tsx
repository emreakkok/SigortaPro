import { useState } from "react";
import { Link } from "react-router-dom";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import { usePolicyList } from "@/features/policies/hooks/usePolicies";
import { Alert, Button, Card, CardContent, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import {
  INSURANCE_BRANCH_LABELS,
  PolicyStatus,
  type InsuranceBranch,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/**
 * Poliçe durum sekmeleri. Aktif poliçeler ile pasif durumlar (süresi dolmuş/iptal) ayrı sekmelerde;
 * her sekme tek bir backend durum filtresine (veya filtresizliğe) karşılık gelir.
 */
const TABS: { label: string; status: PolicyStatus | undefined }[] = [
  { label: "Aktif", status: PolicyStatus.Active },
  { label: "Süresi Dolmuş", status: PolicyStatus.Expired },
  { label: "İptal", status: PolicyStatus.Cancelled },
  { label: "Tümü", status: undefined },
];

/** Poliçelerim: durum sekmeleri (aktif/pasif), sayfalama ve poliçe kartları. */
export default function PolicyListPage() {
  const [tabIndex, setTabIndex] = useState(0);
  const [page, setPage] = useState(1);

  const status = TABS[tabIndex].status;
  const { data, isLoading, isError, error, isFetching } = usePolicyList({
    page,
    pageSize: 10,
    status,
  });

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Poliçelerim</h1>
        <p className="text-muted-foreground">Aktif ve geçmiş poliçelerinizi görüntüleyin, PDF indirin.</p>
      </div>

      <div className="flex flex-wrap gap-2">
        {TABS.map((tab, index) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => {
              setTabIndex(index);
              setPage(1);
            }}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              index === tabIndex
                ? "bg-primary text-primary-foreground"
                : "bg-secondary text-secondary-foreground hover:bg-accent",
            )}
          >
            {tab.label}
          </button>
        ))}
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
            Bu durumda poliçeniz yok.{" "}
            <Link to="/portal/quotes/new" className="font-medium text-primary hover:underline">
              Yeni bir teklif alın.
            </Link>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className={isFetching ? "space-y-3 opacity-60" : "space-y-3"}>
            {data.items.map((policy) => (
              <Link key={policy.id} to={`/portal/policies/${policy.id}`} className="block">
                <Card className="transition-colors hover:border-primary">
                  <CardContent className="flex items-center justify-between gap-4 py-4">
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="truncate font-medium">{policy.productName}</p>
                        <span className="text-xs text-muted-foreground">
                          {INSURANCE_BRANCH_LABELS[policy.branch as InsuranceBranch]}
                        </span>
                      </div>
                      <p className="mt-1 font-mono text-sm text-primary">{policy.policyNumber}</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {formatDate(policy.startDate)} – {formatDate(policy.endDate)} ·{" "}
                        {formatCurrency(policy.totalPremium)}
                      </p>
                    </div>
                    <PolicyStatusBadge status={policy.status} />
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

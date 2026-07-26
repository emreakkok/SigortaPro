import { useState } from "react";
import { Link } from "react-router-dom";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import { usePolicyList } from "@/features/policies/hooks/usePolicies";
import {
  Alert,
  Button,
  Card,
  CardContent,
  EmptyState,
  Pagination,
  ShieldCheckIcon,
  SkeletonRows,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { PORTAL_PAGE_SIZE } from "@/shared/lib/pagination";
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
    pageSize: PORTAL_PAGE_SIZE,
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
        <SkeletonRows rows={4} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ShieldCheckIcon />}
            title="Bu durumda poliçeniz yok"
            description="Bu sekmede görüntülenecek bir poliçeniz bulunmuyor. Yeni bir teklif alarak dakikalar içinde güvence oluşturabilirsiniz."
            action={
              <Link to="/portal/quotes/new">
                <Button>Yeni Teklif Al</Button>
              </Link>
            }
          />
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

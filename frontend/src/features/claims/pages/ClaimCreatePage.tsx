import { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ClaimForm } from "@/features/claims/components/ClaimForm";
import { useCreateClaim } from "@/features/claims/hooks/useClaims";
import { usePolicyList } from "@/features/policies/hooks/usePolicies";
import { PolicyStatus } from "@/shared/types/insurance.types";
import {
  Alert,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/** Hasar bildirimi: aktif poliçelerden birini seçip olay bilgisini girer. */
export default function ClaimCreatePage() {
  const navigate = useNavigate();
  // Yalnızca aktif poliçelere hasar açılabilir (backend iş kuralı); seçenekleri buradan besliyoruz.
  const policies = usePolicyList({ status: PolicyStatus.Active, pageSize: 100 });
  const createClaim = useCreateClaim();

  useEffect(() => {
    if (createClaim.isSuccess) {
      navigate(`/portal/claims/${createClaim.data.id}`);
    }
  }, [createClaim.isSuccess, createClaim.data, navigate]);

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <Link to="/portal/claims" className="text-sm text-primary hover:underline">
          ← Hasarlarıma dön
        </Link>
        <h1 className="mt-2 text-2xl font-bold tracking-tight">Hasar Bildir</h1>
        <p className="text-muted-foreground">Aktif bir poliçeniz için hasar bildiriminde bulunun.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Hasar Bilgileri</CardTitle>
        </CardHeader>
        <CardContent>
          {policies.isLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : policies.isError || policies.data === undefined ? (
            <Alert variant="destructive">{getApiErrorMessages(policies.error)[0]}</Alert>
          ) : policies.data.items.length === 0 ? (
            <Alert>
              Hasar bildirmek için <strong>aktif</strong> bir poliçeniz olmalı.{" "}
              <Link to="/portal/quotes/new" className="font-medium text-primary hover:underline">
                Yeni bir teklif alın.
              </Link>
            </Alert>
          ) : (
            <ClaimForm
              activePolicies={policies.data.items}
              onSubmit={(request) => createClaim.mutate(request)}
              isPending={createClaim.isPending}
              error={createClaim.isError ? createClaim.error : undefined}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

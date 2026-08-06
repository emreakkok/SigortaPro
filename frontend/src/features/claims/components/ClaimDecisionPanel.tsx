import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useRoles } from "@/features/auth/hooks/useRoles";
import {
  useApproveClaim,
  usePayClaim,
  useRejectClaim,
  useStartClaimReview,
} from "@/features/claims/hooks/useClaims";
import {
  approveClaimSchema,
  rejectClaimSchema,
  type ApproveClaimFormValues,
  type RejectClaimFormValues,
} from "@/features/claims/types/claim.schemas";
import type { Claim } from "@/features/claims/types/claim.types";
import {
  Alert,
  Button,
  FormField,
  Input,
  Spinner,
  Textarea,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { ClaimStatus } from "@/shared/types/insurance.types";

/** Bir karar aksiyonunun hata mesajlarını Alert olarak gösterir. */
function ActionError({ error }: { error: unknown }) {
  if (error === null || error === undefined) {
    return null;
  }
  return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
}

/** UnderReview hasar için onay formu: onay tutarı + opsiyonel değerlendirme notu. */
function ApproveForm({ claim }: { claim: Claim }) {
  const approve = useApproveClaim();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ApproveClaimFormValues>({
    resolver: zodResolver(approveClaimSchema),
    defaultValues: { approvedAmount: claim.estimatedAmount },
  });

  const submit = (values: ApproveClaimFormValues) =>
    approve.mutate({
      id: claim.id,
      request: {
        approvedAmount: values.approvedAmount,
        reviewNote: values.reviewNote === "" ? undefined : values.reviewNote,
      },
    });

  return (
    <form className="space-y-3 rounded-lg border p-4" noValidate onSubmit={handleSubmit(submit)}>
      <p className="text-sm font-semibold">Hasarı Onayla</p>
      <ActionError error={approve.error} />
      <FormField
        htmlFor="approvedAmount"
        label="Onaylanan Tutar (₺)"
        error={errors.approvedAmount?.message}
      >
        <Input
          id="approvedAmount"
          type="number"
          step="0.01"
          min="0"
          {...register("approvedAmount")}
        />
      </FormField>
      <FormField
        htmlFor="approveNote"
        label="Değerlendirme Notu (opsiyonel)"
        error={errors.reviewNote?.message}
      >
        <Textarea id="approveNote" rows={2} {...register("reviewNote")} />
      </FormField>
      <Button type="submit" disabled={approve.isPending}>
        {approve.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Onayla"}
      </Button>
    </form>
  );
}

/** UnderReview hasar için ret formu: gerekçe (değerlendirme notu) zorunludur. */
function RejectForm({ claim }: { claim: Claim }) {
  const reject = useRejectClaim();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RejectClaimFormValues>({ resolver: zodResolver(rejectClaimSchema) });

  const submit = (values: RejectClaimFormValues) =>
    reject.mutate({ id: claim.id, request: { reviewNote: values.reviewNote } });

  return (
    <form className="space-y-3 rounded-lg border p-4" noValidate onSubmit={handleSubmit(submit)}>
      <p className="text-sm font-semibold">Hasarı Reddet</p>
      <ActionError error={reject.error} />
      <FormField htmlFor="rejectNote" label="Ret Gerekçesi" error={errors.reviewNote?.message}>
        <Textarea id="rejectNote" rows={2} {...register("reviewNote")} />
      </FormField>
      <Button type="submit" variant="destructive" disabled={reject.isPending}>
        {reject.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Reddet"}
      </Button>
    </form>
  );
}

/**
 * Hasar karar aksiyonları (acente personeli): durum makinesine göre uygun aksiyonu sunar —
 * Submitted → incelemeye al; UnderReview → onayla/reddet; Approved → ödemeyi gerçekleştir.
 * Paid/Rejected uç durumlardır (aksiyon yok). Geçersiz geçişler backend'de 409 döner.
 *
 * (görevler ayrılığı): Ödeme (`pay`) aksiyonu YALNIZCA Admin'e gösterilir; Personel
 * inceleme/onay/ret yapar ama ödeme yapamaz. Bu yalnızca UX gizlemesidir — gerçek kısıt backend'de
 * `[Authorize(Roles = Admin)]` ile sağlanır (Personel doğrudan çağırsa 403 alır).
 */
export function ClaimDecisionPanel({ claim }: { claim: Claim }) {
  const { isAdmin } = useRoles();
  const startReview = useStartClaimReview();
  const pay = usePayClaim();

  if (claim.status === ClaimStatus.Submitted) {
    return (
      <div className="space-y-3">
        <ActionError error={startReview.error} />
        <Button disabled={startReview.isPending} onClick={() => startReview.mutate(claim.id)}>
          {startReview.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "İncelemeye Al"}
        </Button>
      </div>
    );
  }

  if (claim.status === ClaimStatus.UnderReview) {
    return (
      <div className="space-y-4">
        <ApproveForm claim={claim} />
        <RejectForm claim={claim} />
      </div>
    );
  }

  if (claim.status === ClaimStatus.Approved) {
    // Ödeme yalnızca Admin'e görünür. Personel için bilgilendirme gösterilir (aksiyon yok).
    if (!isAdmin) {
      return (
        <p className="text-sm text-muted-foreground">
          Hasar onaylandı. Ödeme işlemi yalnızca yönetici (Admin) tarafından gerçekleştirilebilir.
        </p>
      );
    }

    return (
      <div className="space-y-3">
        <ActionError error={pay.error} />
        <Button disabled={pay.isPending} onClick={() => pay.mutate(claim.id)}>
          {pay.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Ödemeyi Gerçekleştir"}
        </Button>
      </div>
    );
  }

  return (
    <p className="text-sm text-muted-foreground">
      Hasar süreci tamamlandı; başka aksiyon gerekmiyor.
    </p>
  );
}

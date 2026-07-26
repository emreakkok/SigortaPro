import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { StaffStatusBadge } from "@/features/staff/components/StaffStatusBadge";
import { useSetStaffStatus, useStaffDetail, useUpdateStaff } from "@/features/staff/hooks/useStaff";
import {
  updateStaffSchema,
  type UpdateStaffFormValues,
} from "@/features/staff/types/staff.schemas";
import type { StaffDetail } from "@/features/staff/types/staff.types";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/** Ad güncelleme formu — yalnızca `fullName` düzenlenebilir; e-posta ve rol backend'de değiştirilemez. */
function UpdateNameForm({ staff }: { staff: StaffDetail }) {
  const update = useUpdateStaff();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UpdateStaffFormValues>({
    resolver: zodResolver(updateStaffSchema),
    defaultValues: { fullName: staff.fullName ?? "" },
  });

  const submit = (values: UpdateStaffFormValues) =>
    update.mutate({ id: staff.id, request: { fullName: values.fullName } });

  return (
    <form className="space-y-3 rounded-lg border p-4" noValidate onSubmit={handleSubmit(submit)}>
      <p className="text-sm font-semibold">Bilgileri Düzenle</p>
      {update.isError && <Alert variant="destructive">{getApiErrorMessages(update.error)[0]}</Alert>}
      <FormField htmlFor="editFullName" label="Ad Soyad" error={errors.fullName?.message}>
        <Input id="editFullName" {...register("fullName")} />
      </FormField>
      <Button type="submit" disabled={update.isPending}>
        {update.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Kaydet"}
      </Button>
    </form>
  );
}

/** Aktif/pasif yönetimi — mevcut duruma göre "Pasifleştir" / "Aktifleştir" aksiyonu sunar. */
function StatusControl({ staff }: { staff: StaffDetail }) {
  const setStatus = useSetStaffStatus();
  const nextActive = !staff.isActive;

  return (
    <div className="space-y-3 rounded-lg border p-4">
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold">Hesap Durumu</p>
        <StaffStatusBadge isActive={staff.isActive} />
      </div>
      {setStatus.isError && (
        <Alert variant="destructive">{getApiErrorMessages(setStatus.error)[0]}</Alert>
      )}
      <p className="text-xs text-muted-foreground">
        {staff.isActive
          ? "Pasifleştirilen personel giriş yapamaz ve eldeki oturumları iptal edilir."
          : "Aktifleştirilen personel yeniden giriş yapabilir."}
      </p>
      <Button
        variant={staff.isActive ? "destructive" : "default"}
        disabled={setStatus.isPending}
        onClick={() => setStatus.mutate({ id: staff.id, request: { isActive: nextActive } })}
      >
        {setStatus.isPending ? (
          <Spinner className="[&>div]:h-4 [&>div]:w-4" />
        ) : staff.isActive ? (
          "Pasifleştir"
        ) : (
          "Aktifleştir"
        )}
      </Button>
    </div>
  );
}

/** Personel detay çekmecesi içeriği: künye + ad düzenleme + aktif/pasif yönetimi (yalnızca Admin). */
export function StaffDetailPanel({ staffId }: { staffId: string }) {
  const { data, isLoading, isError, error } = useStaffDetail(staffId);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || data === undefined) {
    return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-lg font-semibold">{data.fullName ?? "—"}</p>
          <StaffStatusBadge isActive={data.isActive} />
        </div>
        <p className="text-sm text-muted-foreground">{data.email}</p>
        <p className="text-xs text-muted-foreground">Rol: {data.roles.join(", ")}</p>
      </div>

      <UpdateNameForm staff={data} />
      <StatusControl staff={data} />
    </div>
  );
}

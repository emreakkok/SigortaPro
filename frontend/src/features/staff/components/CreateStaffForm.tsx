import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useCreateStaff } from "@/features/staff/hooks/useStaff";
import {
  createStaffSchema,
  type CreateStaffFormValues,
} from "@/features/staff/types/staff.schemas";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/**
 * Personel oluşturma formu (yalnızca Admin). Alanlar backend DTO'suna birebir uyar: ad soyad, e-posta, şifre.
 * GÜVENLİK: rol seçimi, Admin oluşturma ve isActive seçimi BİLİNÇLİ olarak yoktur — rol backend'de
 * daima `Personel`'e sabittir. Başarıda çağıran (`onCreated`) çekmeceyi kapatır; liste hook'ta tazelenir.
 */
export function CreateStaffForm({ onCreated }: { onCreated: () => void }) {
  const create = useCreateStaff();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateStaffFormValues>({ resolver: zodResolver(createStaffSchema) });

  const submit = (values: CreateStaffFormValues) =>
    create.mutate(values, { onSuccess: () => onCreated() });

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit(submit)}>
      {create.isError && <Alert variant="destructive">{getApiErrorMessages(create.error)[0]}</Alert>}

      <FormField htmlFor="staffFullName" label="Ad Soyad" error={errors.fullName?.message}>
        <Input id="staffFullName" autoComplete="off" {...register("fullName")} />
      </FormField>

      <FormField htmlFor="staffEmail" label="E-posta" error={errors.email?.message}>
        <Input id="staffEmail" type="email" autoComplete="off" {...register("email")} />
      </FormField>

      <FormField htmlFor="staffPassword" label="Geçici Şifre" error={errors.password?.message}>
        <Input id="staffPassword" type="password" autoComplete="new-password" {...register("password")} />
      </FormField>

      <p className="text-xs text-muted-foreground">
        Rol otomatik olarak <span className="font-medium">Personel</span> atanır. Personel ilk girişten sonra
        şifresini kendisi değiştirebilir.
      </p>

      <Button type="submit" className="w-full" disabled={create.isPending}>
        {create.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Personel Oluştur"}
      </Button>
    </form>
  );
}

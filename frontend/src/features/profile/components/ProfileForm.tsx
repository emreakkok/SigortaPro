import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useUpdateProfile } from "@/features/profile/hooks/useProfile";
import { profileSchema, type ProfileFormValues } from "@/features/profile/types/profile.schemas";
import type { CustomerProfile } from "@/features/profile/types/profile.types";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/** Profil düzenleme formu: ad/soyad, telefon ve adres (TCKN ve doğum tarihi değiştirilemez). */
export function ProfileForm({ profile }: { profile: CustomerProfile }) {
  const updateMutation = useUpdateProfile();
  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      firstName: profile.firstName,
      lastName: profile.lastName,
      phoneNumber: profile.phoneNumber,
      city: profile.address.city,
      district: profile.address.district,
      neighborhood: profile.address.neighborhood,
      postalCode: profile.address.postalCode,
    },
  });

  const serverErrors = updateMutation.isError ? getApiErrorMessages(updateMutation.error) : [];

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit((v) => updateMutation.mutate(v))}>
      {serverErrors.length > 0 && (
        <Alert variant="destructive">
          <ul className="list-inside space-y-1">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </Alert>
      )}
      {updateMutation.isSuccess && !isDirty && (
        <Alert variant="success">Profiliniz güncellendi.</Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="firstName" label="Ad" error={errors.firstName?.message}>
          <Input id="firstName" autoComplete="given-name" {...register("firstName")} />
        </FormField>
        <FormField htmlFor="lastName" label="Soyad" error={errors.lastName?.message}>
          <Input id="lastName" autoComplete="family-name" {...register("lastName")} />
        </FormField>
      </div>

      <FormField htmlFor="phoneNumber" label="Telefon" error={errors.phoneNumber?.message}>
        <Input id="phoneNumber" type="tel" placeholder="+905XXXXXXXXX" {...register("phoneNumber")} />
      </FormField>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="city" label="İl" error={errors.city?.message}>
          <Input id="city" {...register("city")} />
        </FormField>
        <FormField htmlFor="district" label="İlçe" error={errors.district?.message}>
          <Input id="district" {...register("district")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="neighborhood" label="Mahalle" error={errors.neighborhood?.message}>
          <Input id="neighborhood" {...register("neighborhood")} />
        </FormField>
        <FormField htmlFor="postalCode" label="Posta Kodu" error={errors.postalCode?.message}>
          <Input id="postalCode" inputMode="numeric" {...register("postalCode")} />
        </FormField>
      </div>

      <Button type="submit" disabled={updateMutation.isPending}>
        {updateMutation.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Kaydet"}
      </Button>
    </form>
  );
}

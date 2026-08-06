import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import {
  propertySchema,
  type PropertyFormValues,
} from "@/features/profile/types/profile.schemas";
import { Alert, Button, CityCombobox, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

interface PropertyFormProps {
  onSubmit: (values: PropertyFormValues) => void;
  isPending: boolean;
  error?: unknown;
  submitLabel: string;
  onCancel?: () => void;
}


/** Konut ekleme formu. */
export function PropertyForm({
  onSubmit,
  isPending,
  error,
  submitLabel,
  onCancel,
}: PropertyFormProps) {
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<PropertyFormValues>({
    resolver: zodResolver(propertySchema),
  });

  const serverErrors = error !== undefined ? getApiErrorMessages(error) : [];

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit(onSubmit)}>
      {serverErrors.length > 0 && (
        <Alert variant="destructive">
          <ul className="list-inside space-y-1">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="city" label="İl" error={errors.city?.message}>
          <Controller
            control={control}
            name="city"
            render={({ field }) => (
              <CityCombobox id="city" value={field.value ?? ""} onChange={field.onChange} />
            )}
          />
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

      <div className="grid gap-4 sm:grid-cols-3">
        <FormField htmlFor="buildingAge" label="Bina Yaşı" error={errors.buildingAge?.message}>
          <Input id="buildingAge" type="number" inputMode="numeric" {...register("buildingAge")} />
        </FormField>
        <FormField htmlFor="squareMeters" label="Metrekare" error={errors.squareMeters?.message}>
          <Input id="squareMeters" type="number" inputMode="numeric" {...register("squareMeters")} />
        </FormField>
      </div>

      {/* Deprem bölgesi kullanıcı beyanı değildir; adresin ilinden sistem türetir. */}
      <div className="rounded-lg border bg-muted/30 px-4 py-3">
        <p className="text-sm text-muted-foreground">
          Deprem bölgesi, adresinizin il bilgisine göre otomatik belirlenir.
        </p>
      </div>

      <div className="flex gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : submitLabel}
        </Button>
        {onCancel !== undefined && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isPending}>
            İptal
          </Button>
        )}
      </div>
    </form>
  );
}

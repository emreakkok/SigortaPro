import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  propertySchema,
  type PropertyFormValues,
} from "@/features/profile/types/profile.schemas";
import { Alert, Button, FormField, Input, Select, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

interface PropertyFormProps {
  onSubmit: (values: PropertyFormValues) => void;
  isPending: boolean;
  error?: unknown;
  submitLabel: string;
  onCancel?: () => void;
}

const EARTHQUAKE_ZONES = [1, 2, 3, 4, 5];

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

      <div className="grid gap-4 sm:grid-cols-3">
        <FormField htmlFor="buildingAge" label="Bina Yaşı" error={errors.buildingAge?.message}>
          <Input id="buildingAge" type="number" inputMode="numeric" {...register("buildingAge")} />
        </FormField>
        <FormField htmlFor="squareMeters" label="Metrekare" error={errors.squareMeters?.message}>
          <Input id="squareMeters" type="number" inputMode="numeric" {...register("squareMeters")} />
        </FormField>
        <FormField
          htmlFor="earthquakeZone"
          label="Deprem Bölgesi"
          error={errors.earthquakeZone?.message}
        >
          <Select id="earthquakeZone" defaultValue="" {...register("earthquakeZone")}>
            <option value="" disabled>
              Seçiniz
            </option>
            {EARTHQUAKE_ZONES.map((zone) => (
              <option key={zone} value={zone}>
                {zone}. Bölge
              </option>
            ))}
          </Select>
        </FormField>
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

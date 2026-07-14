import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  vehicleSchema,
  type VehicleFormValues,
} from "@/features/profile/types/profile.schemas";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

interface VehicleFormProps {
  defaultValues?: Partial<VehicleFormValues>;
  onSubmit: (values: VehicleFormValues) => void;
  isPending: boolean;
  /** Sunucu hatası (mutation.error); varsa formun üstünde gösterilir. */
  error?: unknown;
  submitLabel: string;
  onCancel?: () => void;
}

/** Araç ekleme/güncelleme formu (aynı bileşen; mod, parent'ın verdiği onSubmit ile belirlenir). */
export function VehicleForm({
  defaultValues,
  onSubmit,
  isPending,
  error,
  submitLabel,
  onCancel,
}: VehicleFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<VehicleFormValues>({
    resolver: zodResolver(vehicleSchema),
    defaultValues,
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

      <FormField htmlFor="plateNumber" label="Plaka" error={errors.plateNumber?.message}>
        <Input id="plateNumber" placeholder="34 ABC 123" {...register("plateNumber")} />
      </FormField>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="brand" label="Marka" error={errors.brand?.message}>
          <Input id="brand" placeholder="Toyota" {...register("brand")} />
        </FormField>
        <FormField htmlFor="model" label="Model" error={errors.model?.message}>
          <Input id="model" placeholder="Corolla" {...register("model")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField
          htmlFor="manufactureYear"
          label="Üretim Yılı"
          error={errors.manufactureYear?.message}
        >
          <Input id="manufactureYear" type="number" inputMode="numeric" {...register("manufactureYear")} />
        </FormField>
        <FormField
          htmlFor="enginePowerHp"
          label="Motor Gücü (HP)"
          error={errors.enginePowerHp?.message}
        >
          <Input id="enginePowerHp" type="number" inputMode="numeric" {...register("enginePowerHp")} />
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

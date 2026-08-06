import { useEffect, useMemo, useRef, useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { useVehicleCatalog } from "@/features/profile/hooks/useVehicleCatalog";
import {
  vehicleSchema,
  type VehicleFormValues,
} from "@/features/profile/types/profile.schemas";
import { Alert, Button, Combobox, FormField, Input, Select, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  VEHICLE_USAGE_DESCRIPTIONS,
  VEHICLE_USAGE_LABELS,
  VehicleUsage,
} from "@/shared/types/insurance.types";

const VEHICLE_USAGE_OPTIONS = Object.values(VehicleUsage) as VehicleUsage[];

interface VehicleFormProps {
  defaultValues?: Partial<VehicleFormValues>;
  onSubmit: (values: VehicleFormValues) => void;
  isPending: boolean;
  /** Sunucu hatası (mutation.error); varsa formun üstünde gösterilir. */
  error?: unknown;
  submitLabel: string;
  onCancel?: () => void;
}

/**
 * Araç ekleme/güncelleme formu. Marka ve model için katalog tabanlı "cascading select"
 * (aranabilir combobox) kullanılır; listede olmayan araçlar için "Diğer"
 * seçeneğiyle serbest metin girişine düşülür. Backend sözleşmesi (brand/model string) değişmez.
 */
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
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<VehicleFormValues>({
    resolver: zodResolver(vehicleSchema),
    defaultValues,
  });

  const { data: catalog, isError: catalogFailed } = useVehicleCatalog();
  const brands = useMemo(() => catalog?.brands ?? [], [catalog]);
  const brandNames = useMemo(() => brands.map((brand) => brand.name), [brands]);

  const [useCustom, setUseCustom] = useState(false);
  const initializedRef = useRef(false);

  // Düzenlemede: mevcut marka katalogda yoksa başlangıçta "Diğer" (serbest metin) moduna geç.
  useEffect(() => {
    if (initializedRef.current || catalog === undefined) {
      return;
    }
    initializedRef.current = true;
    const brandDefault = defaultValues?.brand;
    if (
      typeof brandDefault === "string" &&
      brandDefault.length > 0 &&
      !catalog.brands.some((brand) => brand.name === brandDefault)
    ) {
      setUseCustom(true);
    }
  }, [catalog, defaultValues]);

  // Katalog yüklenemezse (ağ hatası) forma engel olmamak için serbest metne düşülür.
  const customMode = useCustom || catalogFailed;

  const selectedBrand = watch("brand");
  const models = useMemo(
    () => brands.find((brand) => brand.name === selectedBrand)?.models ?? [],
    [brands, selectedBrand],
  );

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

      {customMode ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField htmlFor="brand" label="Marka" error={errors.brand?.message}>
            <Input id="brand" placeholder="Toyota" {...register("brand")} />
          </FormField>
          <FormField htmlFor="model" label="Model" error={errors.model?.message}>
            <Input id="model" placeholder="Corolla" {...register("model")} />
          </FormField>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField htmlFor="brand" label="Marka" error={errors.brand?.message}>
            <Controller
              control={control}
              name="brand"
              render={({ field }) => (
                <Combobox
                  id="brand"
                  value={field.value ?? ""}
                  options={brandNames}
                  placeholder="Marka seçin veya arayın"
                  onChange={(value) => {
                    field.onChange(value);
                    // Marka değişince model seçimi sıfırlanır (cascading).
                    setValue("model", "", { shouldValidate: false });
                  }}
                />
              )}
            />
          </FormField>
          <FormField htmlFor="model" label="Model" error={errors.model?.message}>
            <Controller
              control={control}
              name="model"
              render={({ field }) => (
                <Combobox
                  id="model"
                  value={field.value ?? ""}
                  options={models}
                  disabled={selectedBrand === undefined || selectedBrand === ""}
                  placeholder={
                    selectedBrand ? "Model seçin veya arayın" : "Önce marka seçin"
                  }
                  emptyText="Bu marka için model bulunamadı."
                  onChange={field.onChange}
                />
              )}
            />
          </FormField>
        </div>
      )}

      {!catalogFailed && (
        <label className="flex items-center gap-2 text-sm text-muted-foreground">
          <input
            type="checkbox"
            className="accent-primary"
            checked={useCustom}
            onChange={(event) => setUseCustom(event.target.checked)}
          />
          Marka/modelim listede yok (elle gireceğim)
        </label>
      )}
      {catalogFailed && (
        <p className="text-sm text-muted-foreground">
          Araç kataloğu yüklenemedi; marka ve modeli elle girebilirsiniz.
        </p>
      )}

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

      {/*
        Kullanım amacı Kasko/Trafik primini doğrudan etkiler. Varsayılan seçim YOKTUR
        ("Seçiniz" disabled) — kullanıcı bilinçli olarak seçer; riskli bir seçenek öne alınmaz.
      */}
      <FormField
        htmlFor="usagePurpose"
        label="Kullanım Amacı"
        error={errors.usagePurpose?.message}
      >
        <Select id="usagePurpose" defaultValue="" {...register("usagePurpose")}>
          <option value="" disabled>
            Seçiniz
          </option>
          {VEHICLE_USAGE_OPTIONS.map((usage) => (
            <option key={usage} value={usage}>
              {VEHICLE_USAGE_LABELS[usage]} — {VEHICLE_USAGE_DESCRIPTIONS[usage]}
            </option>
          ))}
        </Select>
        <p className="mt-1 text-xs text-muted-foreground">
          Kasko ve Trafik priminizi etkiler. Ticari ve taksi kullanım daha yüksek risk taşır.
        </p>
      </FormField>

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

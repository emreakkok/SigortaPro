import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { CardPreview, type CardField } from "@/features/payments/components/CardPreview";
import {
  paymentSchema,
  type PaymentFormValues,
} from "@/features/payments/types/payment.schemas";
import type { InstallmentOption } from "@/features/payments/types/payment.types";
import { formatCardNumberInput } from "@/features/payments/utils/card";
import { Alert, Button, FormField, Input, Label, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import { formatCurrency } from "@/shared/utils/format";

interface PaymentFormProps {
  installmentOptions: InstallmentOption[];
  onSubmit: (values: PaymentFormValues) => void;
  isPending: boolean;
  /** Ödeme reddi (402) dahil sunucu hatası; varsa formun üstünde gösterilir. */
  error?: unknown;
}

/** Form alanı adını kart görselindeki bölgeye eşler (ay/yıl tek "expiry" bölgesidir). */
function cardFieldOf(name: string): CardField | null {
  if (name === "cardNumber" || name === "cardHolderName" || name === "cvv") {
    return name;
  }
  if (name === "expiryMonth" || name === "expiryYear") {
    return "expiry";
  }
  return null;
}

/** Kart bilgileri + taksit seçimi formu. Yapısal doğrulama Zod ile; ödeme sonucu backend'de belirlenir. */
export function PaymentForm({ installmentOptions, onSubmit, isPending, error }: PaymentFormProps) {
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<PaymentFormValues>({
    resolver: zodResolver(paymentSchema),
    defaultValues: { installmentCount: 1 },
  });

  // Kart görselinde vurgulanacak alan — türetilemeyen tek UI durumu.
  const [focusedField, setFocusedField] = useState<CardField | null>(null);

  // Kart görseli doğrudan form değerlerinden beslenir (ek state yok).
  const [cardNumber, cardHolderName, expiryMonth, expiryYear, cvv] = watch([
    "cardNumber",
    "cardHolderName",
    "expiryMonth",
    "expiryYear",
    "cvv",
  ]);

  const serverErrors = error !== undefined ? getApiErrorMessages(error) : [];
  const selectedCount = Number(watch("installmentCount"));

  // Kart numarası 4'erli maskelenir; paymentSchema gönderim öncesi boşlukları strip eder → sözleşme değişmez.
  const cardNumberField = register("cardNumber");

  return (
    <form className="space-y-6" noValidate onSubmit={handleSubmit(onSubmit)}>
      {serverErrors.length > 0 && (
        <Alert variant="destructive">
          <ul className="list-inside space-y-1">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </Alert>
      )}

      <CardPreview
        cardNumber={cardNumber ?? ""}
        cardHolderName={cardHolderName ?? ""}
        expiryMonth={expiryMonth ?? ""}
        expiryYear={expiryYear ?? ""}
        cvv={cvv ?? ""}
        focusedField={focusedField}
      />

      {/*
        Odak izleme tek yerde: focus/blur olayları bu kapsayıcıda yakalanır (React'te bubble eder), böylece
        her input'a ayrı handler eklenmez ve RHF'nin kendi onBlur'u bozulmaz. Kapsayıcı içinde alan
        değiştirilirken kart geri dönmesin diye blur yalnızca odak dışarı çıktığında temizlenir.
      */}
      <div
        className="space-y-6"
        onFocus={(event) => {
          // Olay kapsayıcıda yakalandığından `target` gerçek alandır; adı attribute'tan okunur (tip güvenli).
          const fieldName = (event.target as HTMLElement).getAttribute("name") ?? "";
          setFocusedField(cardFieldOf(fieldName));
        }}
        onBlur={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget)) {
            setFocusedField(null);
          }
        }}
      >
        <FormField htmlFor="cardHolderName" label="Kart Sahibi" error={errors.cardHolderName?.message}>
          <Input id="cardHolderName" placeholder="Ad Soyad" autoComplete="off" {...register("cardHolderName")} />
        </FormField>

        <FormField htmlFor="cardNumber" label="Kart Numarası" error={errors.cardNumber?.message}>
          <Input
            id="cardNumber"
            inputMode="numeric"
            placeholder="4111 1111 1111 1111"
            autoComplete="off"
            maxLength={23}
            {...cardNumberField}
            onChange={(event) => {
              event.target.value = formatCardNumberInput(event.target.value);
              void cardNumberField.onChange(event);
            }}
          />
        </FormField>

        <div className="grid gap-4 sm:grid-cols-3">
          <FormField htmlFor="expiryMonth" label="Ay (AA)" error={errors.expiryMonth?.message}>
            <Input id="expiryMonth" inputMode="numeric" placeholder="12" maxLength={2} {...register("expiryMonth")} />
          </FormField>
          <FormField htmlFor="expiryYear" label="Yıl (YYYY)" error={errors.expiryYear?.message}>
            <Input id="expiryYear" inputMode="numeric" placeholder="2030" maxLength={4} {...register("expiryYear")} />
          </FormField>
          <FormField htmlFor="cvv" label="CVV" error={errors.cvv?.message}>
            <Input id="cvv" inputMode="numeric" placeholder="123" maxLength={4} autoComplete="off" {...register("cvv")} />
          </FormField>
        </div>

        <div className="space-y-2">
          <Label>Taksit Seçeneği</Label>
          <div className="grid gap-2 sm:grid-cols-2">
            {installmentOptions.map((option) => {
              const isSelected = selectedCount === option.count;
              return (
                <label
                  key={option.count}
                  className={cn(
                    "flex cursor-pointer items-center justify-between rounded-md border p-3 text-sm transition-colors",
                    isSelected ? "border-primary bg-accent" : "hover:border-primary/50",
                  )}
                >
                  <span className="flex items-center gap-2">
                    <input
                      type="radio"
                      value={option.count}
                      className="accent-primary"
                      {...register("installmentCount")}
                    />
                    <span className="font-medium">
                      {option.count === 1 ? "Tek Çekim" : `${option.count} Taksit`}
                    </span>
                  </span>
                  <span className="text-muted-foreground">
                    {option.count === 1
                      ? formatCurrency(option.totalAmount)
                      : `${option.count} × ${formatCurrency(option.monthlyAmount)}`}
                  </span>
                </label>
              );
            })}
          </div>
          {errors.installmentCount?.message !== undefined && (
            <p className="text-sm text-destructive">{errors.installmentCount.message}</p>
          )}
        </div>

        <PaymentSummary options={installmentOptions} selectedCount={selectedCount} />
      </div>

      <Button type="submit" disabled={isPending} className="w-full">
        {isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Ödemeyi Tamamla"}
      </Button>

      <p className="flex items-center justify-center gap-1 text-center text-xs text-muted-foreground">
        Kart bilgileriniz saklanmaz; yalnızca son 4 hane maskeli olarak kaydedilir.
      </p>
    </form>
  );
}

/**
 * Ödeme özeti: kullanıcı hiçbir hesap yapmadan seçili plana göre aylık tutarı ve
 * ödenecek toplamı görür. Değerler backend'in taksit önizlemesinden gelir (frontend hesap yapmaz);
 * mock POS faizsiz olduğundan vergi/faiz kalemi yoktur ve toplam prim ile ödenecek tutar eşittir.
 */
function PaymentSummary({
  options,
  selectedCount,
}: {
  options: InstallmentOption[];
  selectedCount: number;
}) {
  const selected = options.find((option) => option.count === selectedCount);
  if (selected === undefined) {
    return null;
  }

  return (
    <div className="space-y-2 rounded-lg border bg-muted/40 p-4 text-sm">
      <p className="font-semibold">Ödeme Özeti</p>
      <div className="flex justify-between">
        <span className="text-muted-foreground">Toplam Prim</span>
        <span>{formatCurrency(selected.totalAmount)}</span>
      </div>
      <div className="flex justify-between">
        <span className="text-muted-foreground">Ödeme Planı</span>
        <span>
          {selected.count === 1
            ? "Tek Çekim"
            : `${selected.count} Taksit × ${formatCurrency(selected.monthlyAmount)}/ay`}
        </span>
      </div>
      <div className="flex justify-between">
        <span className="text-muted-foreground">Vergi / Faiz</span>
        <span>Dahil (ek ücret yok)</span>
      </div>
      <div className="flex justify-between border-t pt-2 text-base font-semibold">
        <span>Ödenecek Toplam Tutar</span>
        <span className="text-primary">{formatCurrency(selected.totalAmount)}</span>
      </div>
    </div>
  );
}

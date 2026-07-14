import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  paymentSchema,
  type PaymentFormValues,
} from "@/features/payments/types/payment.schemas";
import type { InstallmentOption } from "@/features/payments/types/payment.types";
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

  const serverErrors = error !== undefined ? getApiErrorMessages(error) : [];
  const selectedCount = Number(watch("installmentCount"));

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

      <FormField htmlFor="cardHolderName" label="Kart Sahibi" error={errors.cardHolderName?.message}>
        <Input id="cardHolderName" placeholder="Ad Soyad" autoComplete="off" {...register("cardHolderName")} />
      </FormField>

      <FormField htmlFor="cardNumber" label="Kart Numarası" error={errors.cardNumber?.message}>
        <Input
          id="cardNumber"
          inputMode="numeric"
          placeholder="4111 1111 1111 1111"
          autoComplete="off"
          {...register("cardNumber")}
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

      <Button type="submit" disabled={isPending} className="w-full">
        {isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Ödemeyi Tamamla"}
      </Button>
    </form>
  );
}

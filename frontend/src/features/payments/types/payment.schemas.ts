import { z } from "zod";

/**
 * İzin verilen taksit sayıları — backend `PaymentOptions.AllowedInstallmentCounts` ile birebir.
 * Gerçek taksit seçenekleri (aylık tutar) `GET /payments/installment-options` ile gelir; bu liste
 * yalnızca istemci tarafı doğrulama içindir.
 */
export const ALLOWED_INSTALLMENT_COUNTS = [1, 3, 6, 9, 12] as const;

/**
 * Kart/ödeme formu şeması — backend `PurchaseQuoteCommandValidator` yapısal kurallarını (400) Türkçe
 * mesajlarıyla aynalar. Luhn ve senaryo bazlı ret gateway'in sorumluluğundadır (402); burada doğrulanmaz.
 */
export const paymentSchema = z.object({
  cardHolderName: z
    .string()
    .trim()
    .min(1, "Kart sahibi adı zorunludur.")
    .max(100, "Kart sahibi adı en fazla 100 karakter olabilir."),
  cardNumber: z
    .string()
    .min(1, "Kart numarası zorunludur.")
    // Boşlukları kaldır; yalnızca 13-19 hane kabul edilir (Luhn gateway'de doğrulanır).
    .transform((value) => value.replace(/\s+/g, ""))
    .pipe(z.string().regex(/^\d{13,19}$/, "Kart numarası 13-19 haneli olmalıdır.")),
  expiryMonth: z
    .string()
    .regex(/^(0[1-9]|1[0-2])$/, "Son kullanma ayı 01-12 aralığında olmalıdır."),
  expiryYear: z
    .string()
    .regex(/^\d{4}$/, "Son kullanma yılı 4 haneli olmalıdır."),
  cvv: z
    .string()
    .regex(/^\d{3,4}$/, "CVV 3 veya 4 haneli olmalıdır."),
  installmentCount: z.coerce
    .number()
    .refine(
      (value) => (ALLOWED_INSTALLMENT_COUNTS as readonly number[]).includes(value),
      "Geçersiz taksit sayısı.",
    ),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;

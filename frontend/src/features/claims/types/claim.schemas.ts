import { z } from "zod";

/** Yerel tarih ("2026-07-22") + saat ("10:00") → o anın Date nesnesi (tarayıcının yerel saat dilimi). */
export function combineIncidentDateTime(date: string, time: string): Date {
  return new Date(`${date}T${time}`);
}

/**
 * Hasar bildirim formu şeması — backend `CreateClaimCommandValidator` (yapısal, 400) kurallarını Türkçe
 * mesajlarıyla aynalar. Olay anı **tarih + saat** olarak alınır: teminat penceresi kontrolü backend'de
 * saat hassasiyetlidir (aynı gün poliçe başlangıcından sonraki hasar geçerlidir), bu yüzden saat şarttır.
 * "Gelecek olamaz" burada UX ön kontrolüdür; poliçe dönemi/aktiflik gibi bağlamsal kurallar backend'de
 * (409) doğrulanır (CLAUDE.md §10). Foto metadatası form dışında yönetilir.
 */
export const claimSchema = z
  .object({
    policyId: z.string().min(1, "Poliçe seçilmelidir."),
    incidentDate: z.string().min(1, "Olay tarihi zorunludur."),
    incidentTime: z.string().min(1, "Olay saati zorunludur."),
    description: z
      .string()
      .min(1, "Hasar açıklaması zorunludur.")
      .max(1000, "Hasar açıklaması en fazla 1000 karakter olabilir."),
    estimatedAmount: z.coerce
      .number({ invalid_type_error: "Tahmini tutar sayı olmalıdır." })
      .positive("Tahmini hasar tutarı 0'dan büyük olmalıdır."),
  })
  .refine(
    (values) => {
      const incident = combineIncidentDateTime(values.incidentDate, values.incidentTime);
      return !Number.isNaN(incident.getTime()) && incident <= new Date();
    },
    { message: "Olay zamanı gelecekte olamaz.", path: ["incidentTime"] },
  );

export type ClaimFormValues = z.infer<typeof claimSchema>;

/** Hasar onay formu — backend `ApproveClaimCommandValidator` aynası (tutar > 0, not ≤ 1000). */
export const approveClaimSchema = z.object({
  approvedAmount: z.coerce
    .number({ invalid_type_error: "Onaylanan tutar sayı olmalıdır." })
    .positive("Onaylanan hasar tutarı 0'dan büyük olmalıdır."),
  reviewNote: z
    .string()
    .max(1000, "Değerlendirme notu en fazla 1000 karakter olabilir.")
    .optional(),
});

export type ApproveClaimFormValues = z.infer<typeof approveClaimSchema>;

/** Hasar ret formu — backend `RejectClaimCommandValidator` aynası (gerekçe zorunlu, ≤ 1000). */
export const rejectClaimSchema = z.object({
  reviewNote: z
    .string()
    .min(1, "Ret gerekçesi (değerlendirme notu) zorunludur.")
    .max(1000, "Değerlendirme notu en fazla 1000 karakter olabilir."),
});

export type RejectClaimFormValues = z.infer<typeof rejectClaimSchema>;

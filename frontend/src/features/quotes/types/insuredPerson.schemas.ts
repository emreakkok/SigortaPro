import { z } from "zod";
import { isValidTckn, TURKISH_PHONE_REGEX } from "@/shared/utils/validation";

/**
 * "Başkası adına" sağlık sigortalısı beyan şeması — backend
 * CreateQuoteCommandValidator'ın sigortalı kurallarını Türkçe mesajlarıyla aynalar.
 */
export const insuredPersonSchema = z.object({
  firstName: z
    .string()
    .min(1, "Sigortalı adı zorunludur.")
    .max(100, "Sigortalı adı en fazla 100 karakter olabilir."),
  lastName: z
    .string()
    .min(1, "Sigortalı soyadı zorunludur.")
    .max(100, "Sigortalı soyadı en fazla 100 karakter olabilir."),
  tckn: z
    .string()
    .min(1, "Sigortalı TCKN zorunludur.")
    .refine(isValidTckn, "Geçerli bir sigortalı TCKN giriniz."),
  birthDate: z
    .string()
    .min(1, "Sigortalı doğum tarihi zorunludur.")
    .refine((value) => !Number.isNaN(Date.parse(value)), "Geçerli bir tarih giriniz.")
    .refine((value) => Date.parse(value) < Date.now(), "Doğum tarihi bugünden sonra olamaz."),
  phoneNumber: z
    .string()
    .min(1, "Sigortalı telefon numarası zorunludur.")
    .regex(TURKISH_PHONE_REGEX, "Telefon numarası +90 ile başlamalı ve 10 haneli olmalıdır."),
  relationship: z
    .string()
    .min(1, "Yakınlık derecesi zorunludur.")
    .max(50, "Yakınlık derecesi en fazla 50 karakter olabilir."),
  // "Diğer" seçildiğinde zorunlu serbest açıklama; backend'e relationship olarak bu değer gider.
  relationshipDetail: z
    .string()
    .max(50, "Yakınlık açıklaması en fazla 50 karakter olabilir.")
    .optional(),
}).refine(
  (values) => values.relationship !== "Diğer" || (values.relationshipDetail ?? "").trim().length > 0,
  { message: "Lütfen yakınlık derecesini açıklayın.", path: ["relationshipDetail"] },
);

export type InsuredPersonFormValues = z.infer<typeof insuredPersonSchema>;

/** Form değerini backend beyanına çevirir: "Diğer" seçildiyse açıklama yakınlık olarak gönderilir. */
export function resolveRelationship(values: InsuredPersonFormValues): string {
  return values.relationship === "Diğer" && (values.relationshipDetail ?? "").trim().length > 0
    ? values.relationshipDetail!.trim()
    : values.relationship;
}

/** Gerçek sağlık sigortası akışlarındaki yaygın yakınlık dereceleri (serbest seçim listesi). */
export const RELATIONSHIP_OPTIONS = [
  "Eş",
  "Çocuk",
  "Anne",
  "Baba",
  "Kardeş",
  "Diğer",
] as const;

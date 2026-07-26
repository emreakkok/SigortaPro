import { z } from "zod";

/**
 * Personel oluşturma formu şeması — backend `CreateStaffUserCommandValidator` (400) kurallarını Türkçe
 * mesajlarıyla aynalar. GÜVENLİK: rol/isActive alanı YOKTUR; rol backend'de daima `Personel`'e sabittir.
 * Şifre politikası kayıt (register) ile birebir aynıdır.
 */
export const createStaffSchema = z.object({
  fullName: z
    .string()
    .min(2, "Ad soyad en az 2 karakter olmalıdır.")
    .max(100, "Ad soyad en fazla 100 karakter olabilir."),
  email: z
    .string()
    .min(1, "E-posta adresi zorunludur.")
    .email("Geçerli bir e-posta adresi giriniz.")
    .max(256, "E-posta adresi en fazla 256 karakter olabilir."),
  password: z
    .string()
    .min(8, "Şifre en az 8 karakter olmalıdır.")
    .regex(/[A-Z]/, "Şifre en az bir büyük harf içermelidir.")
    .regex(/[a-z]/, "Şifre en az bir küçük harf içermelidir.")
    .regex(/[0-9]/, "Şifre en az bir rakam içermelidir.")
    .regex(/[^a-zA-Z0-9]/, "Şifre en az bir özel karakter içermelidir."),
});

export type CreateStaffFormValues = z.infer<typeof createStaffSchema>;

/** Personel güncelleme formu — backend `UpdateStaffUserCommandValidator` aynası (yalnızca ad). */
export const updateStaffSchema = z.object({
  fullName: z
    .string()
    .min(2, "Ad soyad en az 2 karakter olmalıdır.")
    .max(100, "Ad soyad en fazla 100 karakter olabilir."),
});

export type UpdateStaffFormValues = z.infer<typeof updateStaffSchema>;

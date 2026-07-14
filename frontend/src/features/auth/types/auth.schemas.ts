import { z } from "zod";
import { isValidTckn, TURKISH_PHONE_REGEX } from "@/shared/utils/validation";

/**
 * Client-side ön doğrulama şemaları — kurallar ve Türkçe mesajlar backend
 * FluentValidation validator'larını (LoginCommandValidator/RegisterCommandValidator)
 * aynalar. Son söz her zaman backend'dedir (DEVELOPMENT_RULES.md §7).
 */
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, "E-posta adresi zorunludur.")
    .email("Geçerli bir e-posta adresi giriniz."),
  password: z.string().min(1, "Şifre zorunludur."),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
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
  firstName: z.string().min(1, "Ad zorunludur.").max(100, "Ad en fazla 100 karakter olabilir."),
  lastName: z
    .string()
    .min(1, "Soyad zorunludur.")
    .max(100, "Soyad en fazla 100 karakter olabilir."),
  tckn: z.string().min(1, "TCKN zorunludur.").refine(isValidTckn, "Geçerli bir TCKN giriniz."),
  birthDate: z
    .string()
    .min(1, "Doğum tarihi zorunludur.")
    .refine((value) => !Number.isNaN(Date.parse(value)), "Geçerli bir tarih giriniz.")
    .refine((value) => Date.parse(value) < Date.now(), "Doğum tarihi bugünden sonra olamaz.")
    .refine(isAtLeast18YearsOld, "Sigortalı en az 18 yaşında olmalıdır."),
  phoneNumber: z
    .string()
    .min(1, "Telefon numarası zorunludur.")
    .regex(TURKISH_PHONE_REGEX, "Telefon numarası +90 ile başlamalı ve 10 haneli olmalıdır."),
  city: z.string().min(1, "İl zorunludur.").max(100, "İl en fazla 100 karakter olabilir."),
  district: z
    .string()
    .min(1, "İlçe zorunludur.")
    .max(100, "İlçe en fazla 100 karakter olabilir."),
  neighborhood: z
    .string()
    .min(1, "Mahalle zorunludur.")
    .max(150, "Mahalle en fazla 150 karakter olabilir."),
  postalCode: z
    .string()
    .min(1, "Posta kodu zorunludur.")
    .max(10, "Posta kodu en fazla 10 karakter olabilir."),
});

export type RegisterFormValues = z.infer<typeof registerSchema>;

function isAtLeast18YearsOld(value: string): boolean {
  const birthDate = new Date(value);
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  const monthDiff = today.getMonth() - birthDate.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
    age--;
  }
  return age >= 18;
}

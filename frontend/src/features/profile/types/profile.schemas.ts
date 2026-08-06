import type { VehicleUsage } from "@/shared/types/insurance.types";
import { z } from "zod";
import { TURKISH_PHONE_REGEX, TURKISH_PLATE_REGEX } from "@/shared/utils/validation";

/**
 * Client-side ön doğrulama şemaları — backend FluentValidation kurallarını
 * (UpdateProfile/AddVehicle/AddProperty validator'ları) Türkçe mesajlarıyla aynalar.
 * Son söz backend'dedir.
 */
const CURRENT_YEAR = new Date().getFullYear();
const MIN_MANUFACTURE_YEAR = 1950;
const MAX_ENGINE_POWER_HP = 2000;
const MAX_BUILDING_AGE = 200;
const MAX_SQUARE_METERS = 10000;

const addressFields = {
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
};

export const profileSchema = z.object({
  firstName: z.string().min(1, "Ad zorunludur.").max(100, "Ad en fazla 100 karakter olabilir."),
  lastName: z
    .string()
    .min(1, "Soyad zorunludur.")
    .max(100, "Soyad en fazla 100 karakter olabilir."),
  phoneNumber: z
    .string()
    .min(1, "Telefon numarası zorunludur.")
    .regex(TURKISH_PHONE_REGEX, "Telefon numarası +90 ile başlamalı ve 10 haneli olmalıdır."),
  ...addressFields,
});
export type ProfileFormValues = z.infer<typeof profileSchema>;

export const vehicleSchema = z.object({
  plateNumber: z
    .string()
    .min(1, "Plaka zorunludur.")
    .regex(TURKISH_PLATE_REGEX, "Geçerli bir Türk plakası giriniz (örn. 34 ABC 123)."),
  brand: z.string().min(1, "Marka zorunludur.").max(100, "Marka en fazla 100 karakter olabilir."),
  model: z.string().min(1, "Model zorunludur.").max(100, "Model en fazla 100 karakter olabilir."),
  manufactureYear: z.coerce
    .number({ invalid_type_error: "Üretim yılı sayı olmalıdır." })
    .int("Üretim yılı tam sayı olmalıdır.")
    .min(MIN_MANUFACTURE_YEAR, `Üretim yılı ${MIN_MANUFACTURE_YEAR} ile ${CURRENT_YEAR + 1} arasında olmalıdır.`)
    .max(CURRENT_YEAR + 1, `Üretim yılı ${MIN_MANUFACTURE_YEAR} ile ${CURRENT_YEAR + 1} arasında olmalıdır.`),
  enginePowerHp: z.coerce
    .number({ invalid_type_error: "Motor gücü sayı olmalıdır." })
    .int("Motor gücü tam sayı olmalıdır.")
    .min(1, `Motor gücü 1 ile ${MAX_ENGINE_POWER_HP} beygir arasında olmalıdır.`)
    .max(MAX_ENGINE_POWER_HP, `Motor gücü 1 ile ${MAX_ENGINE_POWER_HP} beygir arasında olmalıdır.`),
  // Kullanım amacı zorunludur ve varsayılan yoktur — kullanıcı bilinçli seçmelidir.
  // Boş seçim "" olarak gelir; sayıya çevrilemediğinde net bir mesaj gösterilir.
  usagePurpose: z.coerce
    .number({ invalid_type_error: "Kullanım amacı seçilmelidir." })
    .int()
    .min(0, "Kullanım amacı seçilmelidir.")
    .max(2, "Geçersiz kullanım amacı.")
    // Doğrulamadan sonra tip, backend enum aynasına daraltılır (form değeri string gelir).
    .transform((value) => value as VehicleUsage),
});
export type VehicleFormValues = z.infer<typeof vehicleSchema>;

export const propertySchema = z.object({
  ...addressFields,
  buildingAge: z.coerce
    .number({ invalid_type_error: "Bina yaşı sayı olmalıdır." })
    .int("Bina yaşı tam sayı olmalıdır.")
    .min(0, `Bina yaşı 0 ile ${MAX_BUILDING_AGE} arasında olmalıdır.`)
    .max(MAX_BUILDING_AGE, `Bina yaşı 0 ile ${MAX_BUILDING_AGE} arasında olmalıdır.`),
  squareMeters: z.coerce
    .number({ invalid_type_error: "Metrekare sayı olmalıdır." })
    .int("Metrekare tam sayı olmalıdır.")
    .min(1, `Metrekare 1 ile ${MAX_SQUARE_METERS} arasında olmalıdır.`)
    .max(MAX_SQUARE_METERS, `Metrekare 1 ile ${MAX_SQUARE_METERS} arasında olmalıdır.`),
});
export type PropertyFormValues = z.infer<typeof propertySchema>;

// Şifre kuralları backend ChangePasswordCommandValidator (= Register/ResetPassword) ile birebir aynı.
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Mevcut şifre zorunludur."),
    newPassword: z
      .string()
      .min(8, "Şifre en az 8 karakter olmalıdır.")
      .regex(/[A-Z]/, "Şifre en az bir büyük harf içermelidir.")
      .regex(/[a-z]/, "Şifre en az bir küçük harf içermelidir.")
      .regex(/[0-9]/, "Şifre en az bir rakam içermelidir.")
      .regex(/[^a-zA-Z0-9]/, "Şifre en az bir özel karakter içermelidir."),
    confirmPassword: z.string().min(1, "Şifre tekrarı zorunludur."),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Şifreler eşleşmiyor.",
    path: ["confirmPassword"],
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    message: "Yeni şifre mevcut şifreyle aynı olamaz.",
    path: ["newPassword"],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

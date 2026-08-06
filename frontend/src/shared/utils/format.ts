/**
 * TIMEZONE (ADR — timezone stratejisi): Uygulama Türkiye acentesi içindir; instant zaman değerleri
 * daima **Europe/Istanbul** saatiyle gösterilir — viewer'ın tarayıcı saat dilimine GÜVENİLMEZ.
 * Backend instant'ları UTC + "Z" olarak döndürür (T1); `new Date(iso)` doğru anı kurar, aşağıdaki
 * formatlayıcılar bu anı Europe/Istanbul'a çevirir. İleride kullanıcı-bazlı timezone gerekirse yalnızca
 * bu tek sabit parametrelenir.
 */
export const APP_TIME_ZONE = "Europe/Istanbul";

const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  minimumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  timeZone: APP_TIME_ZONE,
});

/** Tutarı Türk Lirası biçiminde gösterir (ör. "24.750,00 ₺"). */
export function formatCurrency(amount: number): string {
  return currencyFormatter.format(amount);
}

/**
 * Bir **instant** (UTC + "Z") değeri, Europe/Istanbul takvim gününe çevirerek gün/ay/yıl gösterir
 * (ör. oluşturulma/başlangıç tarihleri). Date-only alanlar (doğum tarihi) için `formatDateOnly` kullanın.
 */
export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate));
}

/**
 * **Date-only** (takvim günü — doğum tarihi gibi) değeri, timezone dönüşümü YAPMADAN literal olarak gösterir.
 * Backend bu alanları "Z"siz döndürür; instant gibi çevrilmemelidir (gün kayması riski). "YYYY-MM-DD…" →
 * "DD.MM.YYYY". Beklenmeyen biçimde girdi olduğu gibi döner.
 */
export function formatDateOnly(isoDate: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(isoDate);
  if (match === null) {
    return isoDate;
  }
  const [, year, month, day] = match;
  return `${day}.${month}.${year}`;
}

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: APP_TIME_ZONE,
});

/**
 * Bir **instant** (UTC + "Z") değerini Europe/Istanbul saatiyle gün/ay/yıl saat:dakika gösterir
 * (ör. hasar olay anı — saat anlamlıdır).
 */
export function formatDateTime(isoDate: string): string {
  return dateTimeFormatter.format(new Date(isoDate));
}

/**
 * Kanonik telefonu ("+905551111111") okunur biçimde gösterir: "0555 111 11 11".
 * Beklenmeyen biçimde girdi olduğu gibi döner (bozmadan).
 */
export function formatPhoneNumber(phone: string | null | undefined): string {
  if (phone === null || phone === undefined || phone === "") {
    return "";
  }
  const match = /^\+90(\d{3})(\d{3})(\d{2})(\d{2})$/.exec(phone.trim());
  if (match === null) {
    return phone;
  }
  const [, area, part1, part2, part3] = match;
  return `0${area} ${part1} ${part2} ${part3}`;
}

/** Bir çarpanı "×1,25" biçiminde gösterir (prim dökümü için). */
export function formatMultiplier(multiplier: number): string {
  return `×${multiplier.toLocaleString("tr-TR", { minimumFractionDigits: 2 })}`;
}

/**
 * Bir çarpanın prime etkisini kullanıcı diline çevirir:
 * 1.25 → "+%25 ek prim", 0.90 → "−%10 indirim", 1.00 → "Etkisi yok".
 */
export function formatMultiplierEffect(multiplier: number): string {
  const delta = multiplier - 1;
  if (Math.abs(delta) < 0.0001) {
    return "Etkisi yok";
  }
  const percent = Math.abs(delta).toLocaleString("tr-TR", {
    style: "percent",
    maximumFractionDigits: 1,
  });
  return delta > 0 ? `+${percent} ek prim` : `−${percent} indirim`;
}

/**
 * Verilen bitiş tarihine kalan tam gün sayısı (bugünden). Geçmiş tarihte ≤0 döner.
 * Teklif geçerlilik sayacı için kullanılır.
 */
export function daysUntil(isoDate: string): number {
  const target = new Date(isoDate).getTime();
  const now = Date.now();
  return Math.ceil((target - now) / (1000 * 60 * 60 * 24));
}

const percentFormatter = new Intl.NumberFormat("tr-TR", {
  style: "percent",
  maximumFractionDigits: 1,
});

/** 0..1 aralığındaki oranı yüzde biçiminde gösterir (ör. 0.256 → "%25,6"). */
export function formatPercent(ratio: number): string {
  return percentFormatter.format(ratio);
}

const compactCurrencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  notation: "compact",
  maximumFractionDigits: 1,
});

/** Tutarı kısa TL biçiminde gösterir (grafik ekseni için — ör. "₺124,8 B"). */
export function formatCompactCurrency(amount: number): string {
  return compactCurrencyFormatter.format(amount);
}

const monthFormatter = new Intl.DateTimeFormat("tr-TR", {
  month: "short",
  year: "2-digit",
});

/** Yıl+ay değerini kısa ay etiketi olarak gösterir (grafik ekseni için — ör. "Tem 26"). */
export function formatMonthLabel(year: number, month: number): string {
  return monthFormatter.format(new Date(year, month - 1, 1));
}

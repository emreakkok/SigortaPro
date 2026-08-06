/**
 * Kart görseli (CardPreview) için saf biçimlendirme ve marka algılama yardımcıları.
 * Yalnızca sunum amaçlıdır: ödeme mantığı, Luhn doğrulaması ve şema kuralları backend'de/`paymentSchema`'da
 * kalır.
 */
export type CardBrand = "visa" | "mastercard" | "troy" | "unknown";

/** `paymentSchema` 13-19 hane kabul eder; maske de aynı üst sınırı uygular. */
const MAX_CARD_DIGITS = 19;
/** Kart görselinde en az 16 hane yuvası gösterilir (eksikler nokta ile doldurulur). */
const DISPLAY_SLOT_COUNT = 16;
const DOT = "•";

/** Değerden yalnızca rakamları alır (en fazla 19 hane). */
export function extractDigits(value: string): string {
  return value.replace(/\D/g, "").slice(0, MAX_CARD_DIGITS);
}

/**
 * Input maskesi: rakamları 4'erli gruplar hâlinde boşlukla ayırır.
 * Güvenli: `paymentSchema.cardNumber` gönderim öncesi boşlukları zaten strip eder → sözleşme değişmez.
 */
export function formatCardNumberInput(value: string): string {
  return groupByFour(extractDigits(value));
}

/** Kart görselindeki numara: eksik haneler • ile tamamlanır, 4'erli gruplanır. */
export function formatCardNumberDisplay(value: string): string {
  const digits = extractDigits(value);
  const slotCount = Math.max(DISPLAY_SLOT_COUNT, digits.length);
  return groupByFour(digits.padEnd(slotCount, DOT));
}

/** Kart görselindeki son kullanma tarihi: MM/YY (form YYYY alır; kartta son iki hane gösterilir). */
export function formatExpiryDisplay(month: string, year: string): string {
  const monthPart = extractDigits(month).slice(0, 2).padEnd(2, DOT);
  const yearDigits = extractDigits(year);
  // Yılın son iki hanesi ancak 3. hane yazıldığında anlamlıdır; öncesinde nokta gösterilir.
  const yearPart = yearDigits.length >= 3 ? yearDigits.slice(2, 4).padEnd(2, DOT) : DOT.repeat(2);
  return `${monthPart}/${yearPart}`;
}

/** Kart görselindeki CVV: eksik haneler • ile tamamlanır. */
export function formatCvvDisplay(value: string): string {
  return extractDigits(value).slice(0, 4).padEnd(3, DOT);
}

/** Kart sahibi: Türkçe büyük harf (i → İ); boşsa yer tutucu. */
export function formatHolderDisplay(value: string): string {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed.toLocaleUpperCase("tr-TR") : "AD SOYAD";
}

/**
 * Kart markasını BIN önekinden algılar (yazdıkça kademeli): Visa `4`, Mastercard `51-55` ve `2221-2720`,
 * Troy `9792`. Eşleşme yoksa nötr görünüm için "unknown" döner.
 */
export function detectCardBrand(value: string): CardBrand {
  const digits = extractDigits(value);

  if (digits.startsWith("4")) {
    return "visa";
  }
  if (digits.startsWith("9792")) {
    return "troy";
  }
  if (/^5[1-5]/.test(digits)) {
    return "mastercard";
  }
  if (digits.startsWith("2") && digits.length >= 4) {
    const prefix = Number(digits.slice(0, 4));
    if (prefix >= 2221 && prefix <= 2720) {
      return "mastercard";
    }
  }

  return "unknown";
}

function groupByFour(value: string): string {
  return value.replace(/(.{4})/g, "$1 ").trim();
}

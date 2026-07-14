/** Telefon formatı (CODING_STANDARDS.md §6.2): +90 prefiksi + 10 hane. */
export const TURKISH_PHONE_REGEX = /^\+90\d{10}$/;

/**
 * Türk plaka formatı (backend `ValidationPatterns.TurkishPlate` ile birebir):
 * 01-81 il kodu + 1-3 harf (büyük) + 2-4 rakam, opsiyonel boşluklarla.
 */
export const TURKISH_PLATE_REGEX = /^(0[1-9]|[1-7][0-9]|8[01])\s?[A-Z]{1,3}\s?\d{2,4}$/;

/**
 * TCKN algoritmik doğrulaması (backend `TcknValidation` ile aynı kurallar):
 * 11 hane, ilk hane 0 olamaz, 10. ve 11. kontrol basamakları tutmalı.
 * Client-side ön doğrulamadır; son söz her zaman backend'dedir.
 */
export function isValidTckn(value: string): boolean {
  if (!/^[1-9]\d{10}$/.test(value)) {
    return false;
  }

  const digits = value.split("").map(Number);
  const oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
  const evenSum = digits[1] + digits[3] + digits[5] + digits[7];

  // JS'te % negatif kalan üretebilir; backend `TcknValidation` ile aynı normalize etme.
  const tenthDigit = (((oddSum * 7 - evenSum) % 10) + 10) % 10;
  if (digits[9] !== tenthDigit) {
    return false;
  }

  const eleventhDigit = digits.slice(0, 10).reduce((sum, digit) => sum + digit, 0) % 10;
  return digits[10] === eleventhDigit;
}

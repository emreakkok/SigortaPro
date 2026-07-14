const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  minimumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

/** Tutarı Türk Lirası biçiminde gösterir (ör. "24.750,00 ₺"). */
export function formatCurrency(amount: number): string {
  return currencyFormatter.format(amount);
}

/** ISO tarihi gün/ay/yıl biçiminde gösterir. */
export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate));
}

/** Bir çarpanı "×1,25" biçiminde gösterir (prim dökümü için). */
export function formatMultiplier(multiplier: number): string {
  return `×${multiplier.toLocaleString("tr-TR", { minimumFractionDigits: 2 })}`;
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

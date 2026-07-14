import { daysUntil, formatDate } from "@/shared/utils/format";

/** Teklif geçerlilik sayacı: kalan gün sayısı + son geçerlilik tarihi. */
export function QuoteValidity({ validUntil }: { validUntil: string | null }) {
  if (validUntil === null) {
    return null;
  }

  const remaining = daysUntil(validUntil);
  const expired = remaining <= 0;

  return (
    <span className={expired ? "text-muted-foreground" : "text-foreground"}>
      {expired ? "Geçerlilik süresi doldu" : `Son ${remaining} gün geçerli`} ·{" "}
      {formatDate(validUntil)}
    </span>
  );
}

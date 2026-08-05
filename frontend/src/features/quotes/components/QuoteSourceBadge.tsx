import { Badge } from "@/shared/components";
import {
  QUOTE_SOURCE_BADGE_VARIANTS,
  QUOTE_SOURCE_CUSTOMER_LABELS,
  QUOTE_SOURCE_STAFF_LABELS,
  QuoteSource,
} from "@/shared/types/insurance.types";

/**
 * Teklif kaynağı rozeti. `audience` etiketi belirler:
 *  • "customer" → "Kendiniz / Acente" (müşteri yüzeyi; personel kimliği gösterilmez).
 *  • "staff" → "Online / Acente" (acente paneli — kanal görünümü).
 * Self-servis tekliflerde `hideSelf` ile rozet gizlenebilir (müşteri listesinde gürültüyü azaltmak için).
 */
export function QuoteSourceBadge({
  source,
  audience = "staff",
  hideSelf = false,
}: {
  source: QuoteSource;
  audience?: "customer" | "staff";
  hideSelf?: boolean;
}) {
  if (hideSelf && source === QuoteSource.SelfService) {
    return null;
  }

  const label =
    audience === "customer"
      ? QUOTE_SOURCE_CUSTOMER_LABELS[source]
      : QUOTE_SOURCE_STAFF_LABELS[source];

  return <Badge variant={QUOTE_SOURCE_BADGE_VARIANTS[source]}>{label}</Badge>;
}

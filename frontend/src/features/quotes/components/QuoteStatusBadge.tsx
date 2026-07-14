import { Badge } from "@/shared/components";
import {
  QUOTE_STATUS_BADGE_VARIANTS,
  QUOTE_STATUS_LABELS,
  type QuoteStatus,
} from "@/shared/types/insurance.types";

export function QuoteStatusBadge({ status }: { status: QuoteStatus }) {
  return <Badge variant={QUOTE_STATUS_BADGE_VARIANTS[status]}>{QUOTE_STATUS_LABELS[status]}</Badge>;
}

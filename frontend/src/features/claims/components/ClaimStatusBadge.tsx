import { Badge } from "@/shared/components";
import {
  CLAIM_STATUS_BADGE_VARIANTS,
  CLAIM_STATUS_LABELS,
  type ClaimStatus,
} from "@/shared/types/insurance.types";

export function ClaimStatusBadge({ status }: { status: ClaimStatus }) {
  return <Badge variant={CLAIM_STATUS_BADGE_VARIANTS[status]}>{CLAIM_STATUS_LABELS[status]}</Badge>;
}

import { Badge } from "@/shared/components";
import {
  POLICY_STATUS_BADGE_VARIANTS,
  POLICY_STATUS_LABELS,
  type PolicyStatus,
} from "@/shared/types/insurance.types";

export function PolicyStatusBadge({ status }: { status: PolicyStatus }) {
  return <Badge variant={POLICY_STATUS_BADGE_VARIANTS[status]}>{POLICY_STATUS_LABELS[status]}</Badge>;
}

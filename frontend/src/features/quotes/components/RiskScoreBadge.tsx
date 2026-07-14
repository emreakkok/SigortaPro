import { Badge } from "@/shared/components";
import {
  RISK_SCORE_BADGE_VARIANTS,
  RISK_SCORE_LABELS,
  type RiskScore,
} from "@/shared/types/insurance.types";

export function RiskScoreBadge({ score }: { score: RiskScore }) {
  return <Badge variant={RISK_SCORE_BADGE_VARIANTS[score]}>{RISK_SCORE_LABELS[score]}</Badge>;
}

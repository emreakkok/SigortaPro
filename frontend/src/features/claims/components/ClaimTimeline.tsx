import { CLAIM_STATUS_LABELS, ClaimStatus } from "@/shared/types/insurance.types";
import { cn } from "@/shared/lib/utils";

/**
 * Hasar durum zaman çizelgesi. Süreç: Bildirildi → İncelemede → (Onaylandı → Ödendi) | Reddedildi.
 * Reddedilen hasar için üçüncü adım "Reddedildi" (destructive) olur; aksi halde onay/ödeme yolu izlenir.
 */
const NORMAL_PATH: ClaimStatus[] = [
  ClaimStatus.Submitted,
  ClaimStatus.UnderReview,
  ClaimStatus.Approved,
  ClaimStatus.Paid,
];

const REJECTED_PATH: ClaimStatus[] = [
  ClaimStatus.Submitted,
  ClaimStatus.UnderReview,
  ClaimStatus.Rejected,
];

/** Verilen durumun izlediği yoldaki sırası (reached index). */
function reachedIndex(status: ClaimStatus, path: ClaimStatus[]): number {
  return path.indexOf(status);
}

export function ClaimTimeline({ status }: { status: ClaimStatus }) {
  const isRejected = status === ClaimStatus.Rejected;
  const path = isRejected ? REJECTED_PATH : NORMAL_PATH;
  const reached = reachedIndex(status, path);

  return (
    <ol className="space-y-0">
      {path.map((step, index) => {
        const isDone = index < reached;
        const isCurrent = index === reached;
        const isRejectedStep = step === ClaimStatus.Rejected;
        const isLast = index === path.length - 1;

        return (
          <li key={step} className="flex gap-3">
            <div className="flex flex-col items-center">
              <span
                className={cn(
                  "flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 text-xs",
                  isRejectedStep && isCurrent
                    ? "border-destructive bg-destructive text-destructive-foreground"
                    : isDone || isCurrent
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-muted-foreground/30 text-muted-foreground/50",
                )}
              >
                {isDone ? "✓" : isRejectedStep && isCurrent ? "✕" : index + 1}
              </span>
              {!isLast && (
                <span
                  className={cn(
                    "w-0.5 flex-1",
                    index < reached ? "bg-primary" : "bg-muted-foreground/20",
                  )}
                  style={{ minHeight: "1.5rem" }}
                />
              )}
            </div>
            <div className={cn("pb-6", isLast && "pb-0")}>
              <p
                className={cn(
                  "text-sm font-medium",
                  isCurrent
                    ? isRejectedStep
                      ? "text-destructive"
                      : "text-foreground"
                    : isDone
                      ? "text-foreground"
                      : "text-muted-foreground/60",
                )}
              >
                {CLAIM_STATUS_LABELS[step]}
              </p>
              {isCurrent && <p className="text-xs text-muted-foreground">Güncel durum</p>}
            </div>
          </li>
        );
      })}
    </ol>
  );
}

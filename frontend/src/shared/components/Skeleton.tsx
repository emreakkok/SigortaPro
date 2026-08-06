import type { HTMLAttributes } from "react";
import { cn } from "@/shared/lib/utils";

/*
 * Yükleme iskeleti. Çıplak spinner yerine içerik-şekilli placeholder → layout zıplaması
 * olmadan modern bekleme deneyimi. Renk token'dan gelir (`bg-muted`) → Dark Mode uyumlu; `animate-pulse`
 * ile nabız efekti. Boyut/şekil `className` ile verilir.
 */
export function Skeleton({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("animate-pulse rounded-md bg-muted", className)} {...props} />;
}

/** Liste ekranları için tekrar eden kart-satırı iskeleti (poliçe/teklif/hasar listeleri). */
export function SkeletonRows({ rows = 4, className }: { rows?: number; className?: string }) {
  return (
    <div className={cn("space-y-3", className)} aria-hidden="true">
      {Array.from({ length: rows }).map((_, index) => (
        <div key={index} className="rounded-xl border bg-card p-4">
          <div className="flex items-center justify-between gap-4">
            <div className="min-w-0 flex-1 space-y-2">
              <Skeleton className="h-4 w-2/5" />
              <Skeleton className="h-3 w-3/5" />
              <Skeleton className="h-3 w-1/4" />
            </div>
            <Skeleton className="h-6 w-20 rounded-full" />
          </div>
        </div>
      ))}
    </div>
  );
}

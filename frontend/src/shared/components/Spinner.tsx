import type { HTMLAttributes } from "react";
import { cn } from "@/shared/lib/utils";

export type SpinnerProps = HTMLAttributes<HTMLDivElement>;

export function Spinner({ className, ...props }: SpinnerProps) {
  return (
    <div role="status" aria-label="Yükleniyor" className={cn("inline-block", className)} {...props}>
      <div className="h-6 w-6 animate-spin rounded-full border-2 border-muted border-t-primary" />
      <span className="sr-only">Yükleniyor…</span>
    </div>
  );
}

/** Route bazlı lazy yükleme sırasında tam sayfa bekleme göstergesi (Suspense fallback). */
export function FullPageSpinner() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <Spinner />
    </div>
  );
}

import type { ReactNode } from "react";
import { cn } from "@/shared/lib/utils";

interface EmptyStateProps {
  /** Görsel ipucu — genellikle bir tasarım-sistemi ikonu (currentColor → temayla uyumlu). */
  icon?: ReactNode;
  title: string;
  description?: ReactNode;
  /** Birincil aksiyon(lar): buton / link. Boş durumu bir sonraki adıma bağlar. */
  action?: ReactNode;
  className?: string;
}

/*
 * Boş durum bileşeni (ADR-044). "Henüz kayıt yok" düz metni yerine kurumsal SaaS deseni:
 * yumuşak token'lı ikon rozeti + başlık + açıklama + CTA. Renkler token tabanlı (accent/muted/primary)
 * → Dark Mode uyumlu. Bir Card içinde ya da tek başına kullanılabilir.
 */
export function EmptyState({ icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-3 px-6 py-14 text-center",
        className,
      )}
    >
      {icon !== undefined && (
        <span
          aria-hidden="true"
          className="flex h-14 w-14 items-center justify-center rounded-2xl bg-accent text-accent-foreground [&_svg]:h-7 [&_svg]:w-7"
        >
          {icon}
        </span>
      )}
      <div className="space-y-1">
        <p className="text-base font-semibold text-foreground">{title}</p>
        {description !== undefined && (
          <p className="mx-auto max-w-sm text-sm text-muted-foreground">{description}</p>
        )}
      </div>
      {action !== undefined && <div className="mt-1 flex flex-wrap justify-center gap-2">{action}</div>}
    </div>
  );
}

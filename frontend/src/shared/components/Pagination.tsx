import type { ReactNode } from "react";
import { Button } from "@/shared/components/Button";

interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  /** Toplam kayıt sayısı — verilirse "Toplam N kayıt" bağlamı gösterilir (ADR-045). */
  totalCount?: number;
  /** Sol tarafa yerleştirilen ek kontrol — ör. admin sayfa boyutu seçici (ADR-045). */
  children?: ReactNode;
}

/**
 * Önceki/Sonraki sayfalama çubuğu (liste sayfalarının ortak alt bileşeni). Opsiyonel toplam kayıt
 * bağlamı ve sol slot (sayfa boyutu seçici) taşır; mobilde dikey yığılır (ADR-045).
 */
export function Pagination({ page, totalPages, onPageChange, totalCount, children }: PaginationProps) {
  const hasLeft = children !== undefined || totalCount !== undefined;

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      {hasLeft && (
        <div className="flex flex-wrap items-center gap-3">
          {children}
          {totalCount !== undefined && (
            <span className="text-sm text-muted-foreground">
              Toplam <span className="font-medium text-foreground tabular-nums">{totalCount}</span> kayıt
            </span>
          )}
        </div>
      )}
      <div className="flex items-center justify-between gap-2 sm:justify-end">
        <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          Önceki
        </Button>
        <span className="text-sm text-muted-foreground">
          Sayfa {page} / {totalPages === 0 ? 1 : totalPages}
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Sonraki
        </Button>
      </div>
    </div>
  );
}

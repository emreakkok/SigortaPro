import { Button } from "@/shared/components/Button";

interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

/** Önceki/Sonraki sayfalama çubuğu (liste sayfalarının ortak alt bileşeni). */
export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  return (
    <div className="flex items-center justify-between">
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
  );
}

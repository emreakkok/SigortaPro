import { useEffect, useState } from "react";
import { useClaimDocument } from "@/features/claims/hooks/useClaims";
import type { ClaimDocument } from "@/features/claims/types/claim.types";
import { FileTextIcon, Skeleton } from "@/shared/components";

/** Baytları okunur boyuta çevirir (ör. "1,2 MB"). */
function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toLocaleString("tr-TR", { maximumFractionDigits: 1 })} MB`;
}

/**
 * Tek bir hasar belgesi: içerik korumalıdır (Authorization gerektirir), bu yüzden blob olarak çekilip
 * object URL'e çevrilir. Görsel ise thumbnail gösterilir (tıkla → tam boyut yeni sekme); PDF/diğer için
 * "Aç" bağlantısı sunulur. Object URL bileşen kaldırılırken serbest bırakılır.
 */
function ClaimDocumentItem({ claimId, document }: { claimId: string; document: ClaimDocument }) {
  const { data: blob, isLoading, isError } = useClaimDocument(claimId, document.id);
  const [objectUrl, setObjectUrl] = useState<string | null>(null);

  useEffect(() => {
    if (blob === undefined) {
      return;
    }
    const url = URL.createObjectURL(blob);
    setObjectUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [blob]);

  const meta = (
    <div className="min-w-0">
      <p className="truncate text-sm font-medium" title={document.fileName}>
        {document.fileName}
      </p>
      <p className="text-xs text-muted-foreground">{formatFileSize(document.fileSizeBytes)}</p>
    </div>
  );

  if (isLoading || (objectUrl === null && !isError)) {
    return (
      <div className="rounded-lg border p-2">
        <Skeleton className="h-28 w-full rounded-md" />
        <div className="mt-2 space-y-1">
          <Skeleton className="h-3.5 w-2/3" />
          <Skeleton className="h-3 w-1/3" />
        </div>
      </div>
    );
  }

  if (isError || objectUrl === null) {
    return (
      <div className="rounded-lg border p-3">
        {meta}
        <p className="mt-1 text-xs text-destructive">Belge yüklenemedi.</p>
      </div>
    );
  }

  if (document.isImage) {
    return (
      <a
        href={objectUrl}
        target="_blank"
        rel="noreferrer"
        className="group block overflow-hidden rounded-lg border transition-colors hover:border-primary/40"
      >
        <div className="aspect-video overflow-hidden bg-muted">
          <img
            src={objectUrl}
            alt={document.fileName}
            className="h-full w-full object-cover transition-transform duration-200 group-hover:scale-[1.03]"
          />
        </div>
        <div className="p-2">{meta}</div>
      </a>
    );
  }

  return (
    <a
      href={objectUrl}
      target="_blank"
      rel="noreferrer"
      className="flex items-center gap-3 rounded-lg border p-3 transition-colors hover:border-primary/40"
    >
      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
        <FileTextIcon className="h-5 w-5" />
      </span>
      {meta}
      <span className="ml-auto shrink-0 text-sm font-medium text-primary">Aç →</span>
    </a>
  );
}

/**
 * Hasara eklenen belgeler (foto/PDF). Müşteri hasar detayında ve Admin/Personel hasar değerlendirme
 * ekranında AYNI bileşenle gösterilir — erişim yetkisi (sahip müşteri / personel) backend'de doğrulanır.
 */
export function ClaimDocuments({ claimId, documents }: { claimId: string; documents: ClaimDocument[] }) {
  if (documents.length === 0) {
    return null;
  }

  return (
    <section>
      <h3 className="mb-2 text-sm font-semibold text-muted-foreground">
        Ekli Belgeler ({documents.length})
      </h3>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {documents.map((document) => (
          <ClaimDocumentItem key={document.id} claimId={claimId} document={document} />
        ))}
      </div>
    </section>
  );
}

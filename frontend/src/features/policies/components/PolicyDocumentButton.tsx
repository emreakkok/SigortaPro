import { useDownloadPolicyDocument } from "@/features/policies/hooks/usePolicies";
import { Button, Spinner } from "@/shared/components";
import { cn } from "@/shared/lib/utils";

interface PolicyDocumentButtonProps {
  policyId: string;
  policyNumber: string;
  variant?: "default" | "outline";
  className?: string;
}

/** Poliçe PDF'ini indiren buton (ilk indirmede backend belgeyi üretir). */
export function PolicyDocumentButton({
  policyId,
  policyNumber,
  variant = "outline",
  className,
}: PolicyDocumentButtonProps) {
  const download = useDownloadPolicyDocument();

  return (
    <div className={cn("space-y-1", className)}>
      <Button
        type="button"
        variant={variant}
        disabled={download.isPending}
        onClick={() => download.mutate({ policyId, policyNumber })}
      >
        {download.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Poliçe PDF'ini İndir"}
      </Button>
      {download.isError && (
        <p className="text-sm text-destructive">Belge indirilemedi. Lütfen tekrar deneyin.</p>
      )}
    </div>
  );
}

import type { ClaimOperation } from "@/features/dashboard/types/dashboard.types";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/components";
import { formatCurrency } from "@/shared/utils/format";

/**
 * Hasar operasyonu (ADR-052): dönemde bildirilen hasarların durum kırılımı.
 * "Ödenen tutar" YALNIZCA `Paid` kayıtların onay tutarıdır — onaylanmış ama ödenmemiş tutarla karıştırılmaz.
 * "Tahmini tutar" müşteri BEYANIDIR; onaylanan tutar değildir ve öyle sunulmaz.
 */
export function ClaimOperationCard({
  claims,
  showFinancials,
}: {
  claims: ClaimOperation;
  /** P1 D1: ödenen/tahmini hasar tutarı toplamları FİNANSAL → yalnızca Admin. Durum kırılımı operasyoneldir. */
  showFinancials: boolean;
}) {
  const rows = [
    { label: "Bildirildi", value: claims.submitted },
    { label: "İncelemede", value: claims.underReview },
    { label: "Onaylandı", value: claims.approved },
    { label: "Ödendi", value: claims.paid },
    { label: "Reddedildi", value: claims.rejected },
  ];

  const total = rows.reduce((sum, row) => sum + row.value, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Hasar Operasyonu</CardTitle>
        <CardDescription>Bu dönemde bildirilen hasarların güncel durumu.</CardDescription>
      </CardHeader>
      <CardContent>
        {total === 0 ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Seçilen dönemde hasar bildirimi yok.
          </p>
        ) : (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
              {rows.map((row) => (
                <div key={row.label} className="rounded-lg border bg-muted/30 px-3 py-2 text-center">
                  <p className="text-xl font-bold tabular-nums">{row.value}</p>
                  <p className="text-xs text-muted-foreground">{row.label}</p>
                </div>
              ))}
            </div>

            {/* Tutar toplamları FİNANSAL → yalnızca Admin (D1). Durum sayaç kırılımı her rolde görünür. */}
            {showFinancials && (
              <dl className="space-y-1.5 border-t pt-3 text-sm">
                <div className="flex justify-between gap-4">
                  <dt className="text-muted-foreground">Ödenen hasar tutarı</dt>
                  <dd className="font-semibold tabular-nums">{formatCurrency(claims.paidAmount)}</dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt className="text-muted-foreground">Bildirilen tahmini tutar (beyan)</dt>
                  <dd className="tabular-nums">{formatCurrency(claims.estimatedAmount)}</dd>
                </div>
              </dl>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

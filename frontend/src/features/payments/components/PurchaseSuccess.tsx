import { Link } from "react-router-dom";
import { PolicyDocumentButton } from "@/features/policies/components/PolicyDocumentButton";
import type { PurchaseResult } from "@/features/payments/types/payment.types";
import { Button, Card, CardContent, CardHeader, CardTitle } from "@/shared/components";
import { formatCurrency, formatDate } from "@/shared/utils/format";

/** Satın alma sonrası başarı ekranı: poliçe künyesi + PDF indirme + yönlendirmeler. */
export function PurchaseSuccess({ result }: { result: PurchaseResult }) {
  const { policy } = result;

  return (
    <div className="mx-auto max-w-xl space-y-6 text-center">
      <div className="space-y-2">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-success/15 text-2xl text-success">
          ✓
        </div>
        <h1 className="text-2xl font-bold tracking-tight">Ödemeniz alındı</h1>
        <p className="text-muted-foreground">
          Poliçeniz oluşturuldu. Poliçe belgenizi hemen indirebilirsiniz.
        </p>
      </div>

      <Card className="text-left">
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Poliçe</span>
            <span className="font-mono text-base text-primary">{policy.policyNumber}</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm">
          <Row label="Başlangıç" value={formatDate(policy.startDate)} />
          <Row label="Bitiş" value={formatDate(policy.endDate)} />
          <Row label="Ödenen tutar" value={formatCurrency(result.amount)} />
          <Row
            label="Taksit"
            value={result.installmentCount === 1 ? "Tek çekim" : `${result.installmentCount} taksit`}
          />
          <Row label="Kart" value={result.maskedCardNumber} />
          <div className="border-t pt-2">
            <Row label="Toplam prim" value={formatCurrency(policy.totalPremium)} strong />
          </div>
        </CardContent>
      </Card>

      <div className="flex flex-col items-center gap-3">
        <PolicyDocumentButton policyId={policy.id} policyNumber={policy.policyNumber} variant="default" />
        <div className="flex flex-wrap justify-center gap-3">
          <Link to={`/portal/policies/${policy.id}`}>
            <Button variant="outline">Poliçe Detayı</Button>
          </Link>
          <Link to="/portal/policies">
            <Button variant="outline">Poliçelerim</Button>
          </Link>
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className={strong === true ? "text-base font-bold text-primary" : "font-medium"}>{value}</span>
    </div>
  );
}

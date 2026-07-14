import { Link, useParams } from "react-router-dom";
import { PaymentForm } from "@/features/payments/components/PaymentForm";
import { PurchaseSuccess } from "@/features/payments/components/PurchaseSuccess";
import { TestCardHints } from "@/features/payments/components/TestCardHints";
import { useInstallmentOptions, usePurchaseQuote } from "@/features/payments/hooks/usePayments";
import { useQuote } from "@/features/quotes/hooks/useQuotes";
import {
  Alert,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import {
  COVERAGE_PACKAGE_LABELS,
  INSURANCE_BRANCH_LABELS,
  QuoteStatus,
} from "@/shared/types/insurance.types";
import { formatCurrency } from "@/shared/utils/format";

/** Ödeme sayfası: onaylanmış teklifi mock POS ile satın alır. Başarıda başarı ekranı gösterilir. */
export default function PurchasePage() {
  const { id = "" } = useParams();
  const { data: quote, isLoading, isError, error } = useQuote(id);
  const isApproved = quote?.status === QuoteStatus.Approved;

  const installments = useInstallmentOptions(id, isApproved === true);
  const purchase = usePurchaseQuote();

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || quote === undefined) {
    return (
      <div className="mx-auto max-w-xl space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        <BackToQuote id={id} />
      </div>
    );
  }

  // Ödeme başarılı: teklif Purchased oldu, poliçe oluştu → başarı ekranı.
  if (purchase.isSuccess) {
    return <PurchaseSuccess result={purchase.data} />;
  }

  // Yalnızca onaylanmış teklif satın alınabilir (backend guard'ı ile tutarlı UX yönlendirmesi).
  if (!isApproved) {
    return (
      <div className="mx-auto max-w-xl space-y-4">
        <Alert>
          Bu teklif ödeme için uygun değil. Yalnızca <strong>onaylanmış</strong> teklifler satın alınabilir.
        </Alert>
        <BackToQuote id={id} />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <Link to={`/portal/quotes/${id}`} className="text-sm text-primary hover:underline">
          ← Teklife dön
        </Link>
        <h1 className="mt-2 text-2xl font-bold tracking-tight">Ödeme</h1>
        <p className="text-muted-foreground">
          {INSURANCE_BRANCH_LABELS[quote.branch]} · {quote.productName} ·{" "}
          {COVERAGE_PACKAGE_LABELS[quote.coveragePackage]}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-baseline justify-between">
            <span>Ödenecek Tutar</span>
            <span className="text-xl font-bold text-primary">{formatCurrency(quote.totalPremium)}</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <TestCardHints />

          {installments.isLoading ? (
            <div className="flex justify-center py-8">
              <Spinner />
            </div>
          ) : installments.isError || installments.data === undefined ? (
            <Alert variant="destructive">{getApiErrorMessages(installments.error)[0]}</Alert>
          ) : (
            <PaymentForm
              installmentOptions={installments.data}
              onSubmit={(values) =>
                purchase.mutate({
                  quoteId: id,
                  cardNumber: values.cardNumber,
                  cardHolderName: values.cardHolderName,
                  expiryMonth: values.expiryMonth,
                  expiryYear: values.expiryYear,
                  cvv: values.cvv,
                  installmentCount: values.installmentCount,
                })
              }
              isPending={purchase.isPending}
              error={purchase.isError ? purchase.error : undefined}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function BackToQuote({ id }: { id: string }) {
  return (
    <Link to={`/portal/quotes/${id}`} className="text-sm text-primary hover:underline">
      ← Teklife dön
    </Link>
  );
}

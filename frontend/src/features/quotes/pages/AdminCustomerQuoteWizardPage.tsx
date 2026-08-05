import { Link, useNavigate, useParams } from "react-router-dom";
import { useCustomer } from "@/features/customers/hooks/useCustomers";
import { QuoteWizard } from "@/features/quotes/components/wizard/QuoteWizard";
import { Alert, Card, CardContent, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/**
 * Acente destekli teklif sihirbazı: personel/admin, seçtiği müşteri ADINA teklif oluşturur (gerçek akış:
 * müşteri telefonla arar, acente teklifi hazırlar). Ortak QuoteWizard yeniden kullanılır — yalnızca veri
 * kaynağı (seçili müşterinin profili) ve yönlendirme farklıdır. Teklif oluşturulunca acente teklif listesine
 * dönülür ve ilgili teklif açılır. Personel teklifi ONAYLAYAMAZ/SATIN ALAMAZ — bu aksiyonlar müşteriye aittir
 * (backend Customer rolüne kilitli); acente panelindeki teklif detayı zaten salt-okunurdur.
 */
export default function AdminCustomerQuoteWizardPage() {
  const { customerId = "" } = useParams();
  const navigate = useNavigate();
  const { data: profile, isLoading, isError, error } = useCustomer(customerId);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || profile === undefined) {
    return (
      <div className="mx-auto max-w-4xl space-y-4">
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        <Link to="/admin/customers" className="text-sm text-primary hover:underline">
          ← Müşterilere dön
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <Link to="/admin/customers" className="text-sm text-primary hover:underline">
          ← Müşterilere dön
        </Link>
        <h1 className="mt-2 text-2xl font-bold tracking-tight">Müşteri Adına Teklif Oluştur</h1>
        <p className="text-muted-foreground">
          <span className="font-medium text-foreground">
            {profile.firstName} {profile.lastName}
          </span>{" "}
          adına teklif hazırlıyorsunuz.
        </p>
      </div>

      <Card>
        <CardContent className="py-4 text-sm text-muted-foreground">
          Oluşturduğunuz teklif <span className="font-medium text-foreground">müşteriye ait</span> olur.
          Müşteri kendi hesabından inceleyip onaylayabilir, reddedebilir veya satın alabilir. Acente adına
          onay/ödeme yapılamaz.
        </CardContent>
      </Card>

      <QuoteWizard
        profile={profile}
        customerId={customerId}
        onCreated={(quoteId) => navigate(`/admin/quotes?focus=${quoteId}`)}
      />
    </div>
  );
}

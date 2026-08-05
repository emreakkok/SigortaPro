import { useNavigate } from "react-router-dom";
import { useMyProfile } from "@/features/profile/hooks/useProfile";
import { QuoteWizard } from "@/features/quotes/components/wizard/QuoteWizard";
import { Alert, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/** Müşteri self-servis teklif sihirbazı: kendi profili üzerinden anlık teklif alır (ortak QuoteWizard). */
export default function QuoteWizardPage() {
  const navigate = useNavigate();
  const { data: profile, isLoading, isError, error } = useMyProfile();

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || profile === undefined) {
    return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Yeni Teklif</h1>
        <p className="text-muted-foreground">Birkaç adımda anlık teklifinizi alın.</p>
      </div>

      <QuoteWizard
        profile={profile}
        customerId={null}
        onCreated={(quoteId) => navigate(`/portal/quotes/${quoteId}`)}
      />
    </div>
  );
}

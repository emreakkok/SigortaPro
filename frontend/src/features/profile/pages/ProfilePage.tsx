import { useSearchParams } from "react-router-dom";
import { ChangePasswordForm } from "@/features/profile/components/ChangePasswordForm";
import { ProfileForm } from "@/features/profile/components/ProfileForm";
import { PropertiesPanel } from "@/features/profile/components/PropertiesPanel";
import { VehiclesPanel } from "@/features/profile/components/VehiclesPanel";
import { useMyProfile } from "@/features/profile/hooks/useProfile";
import {
  Alert,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Spinner,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import { formatDateOnly } from "@/shared/utils/format";

/*
 * Profil sekmeleri; kimlikler URL `?tab=` değeriyle birebirdir (dropdown menü derin bağlantıları — ADR-040).
 * "Bildirimler" sekmesi kaldırıldı (ADR-046): bildirim sistemi yalnızca staff'a çalışır (SignalR yalnız
 * Admin/Personel oturumunda bağlanır); müşteri hiç bildirim almadığından tercih ekranı yanıltıcıydı.
 */
const TABS = [
  { id: "general", label: "Genel Bilgiler" },
  { id: "vehicles", label: "Araçlarım" },
  { id: "properties", label: "Konutlarım" },
  { id: "password", label: "Şifre Değiştir" },
] as const;

type TabId = (typeof TABS)[number]["id"];

function isTabId(value: string | null): value is TabId {
  return TABS.some((tab) => tab.id === value);
}

/** Profil sayfası: tek sayfada sekmeli hesap yönetimi (genel bilgiler, araçlar, konutlar, şifre — ADR-040). */
export default function ProfilePage() {
  const { data: profile, isLoading, isError, error } = useMyProfile();
  const [searchParams, setSearchParams] = useSearchParams();

  const tabParam = searchParams.get("tab");
  const activeTab: TabId = isTabId(tabParam) ? tabParam : "general";

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
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Hesabım</h1>
        <p className="text-muted-foreground">
          Kişisel bilgilerinizi, risk objelerinizi ve şifrenizi yönetin.
        </p>
      </div>

      <div role="tablist" aria-label="Profil bölümleri" className="flex flex-wrap gap-1 border-b">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            onClick={() => setSearchParams(tab.id === "general" ? {} : { tab: tab.id })}
            className={cn(
              "-mb-px rounded-t-md border-b-2 px-4 py-2 text-sm font-medium transition-colors",
              activeTab === tab.id
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "general" && (
        <Card>
          <CardHeader>
            <CardTitle>Kişisel Bilgiler</CardTitle>
            <CardDescription>
              TCKN {profile.maskedTckn} · Doğum tarihi {formatDateOnly(profile.birthDate)} · E-posta{" "}
              {profile.email ?? "—"} (bu alanlar değiştirilemez)
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ProfileForm profile={profile} />
          </CardContent>
        </Card>
      )}

      {activeTab === "vehicles" && (
        <Card>
          <CardContent className="pt-6">
            <VehiclesPanel vehicles={profile.vehicles} />
          </CardContent>
        </Card>
      )}

      {activeTab === "properties" && (
        <Card>
          <CardContent className="pt-6">
            <PropertiesPanel properties={profile.properties} />
          </CardContent>
        </Card>
      )}

      {activeTab === "password" && (
        <Card>
          <CardHeader>
            <CardTitle>Şifre Değiştir</CardTitle>
            <CardDescription>
              Güvenliğiniz için güçlü bir şifre seçin; şifreniz değiştiğinde mevcut oturumunuz açık kalır.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ChangePasswordForm />
          </CardContent>
        </Card>
      )}
    </div>
  );
}

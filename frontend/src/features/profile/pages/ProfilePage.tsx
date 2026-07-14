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
import { formatDate } from "@/shared/utils/format";

export default function ProfilePage() {
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
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Profilim</h1>
        <p className="text-muted-foreground">Kişisel bilgilerinizi ve risk objelerinizi yönetin.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Kişisel Bilgiler</CardTitle>
          <CardDescription>
            TCKN {profile.maskedTckn} · Doğum tarihi {formatDate(profile.birthDate)} · E-posta{" "}
            {profile.email ?? "—"} (bu alanlar değiştirilemez)
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ProfileForm profile={profile} />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="space-y-8 pt-6">
          <VehiclesPanel vehicles={profile.vehicles} />
          <PropertiesPanel properties={profile.properties} />
        </CardContent>
      </Card>
    </div>
  );
}

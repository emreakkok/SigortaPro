import { Link } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/useAuth";
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/components";

/** Müşteri portalı ana sayfası: aktif özelliklere hızlı erişim + gelecek adımlar. */
export default function PortalHomePage() {
  const { session } = useAuth();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Hoş geldiniz</h1>
        <p className="text-muted-foreground">{session?.email}</p>
      </div>

      <Card className="border-primary/40 bg-accent/40">
        <CardHeader>
          <CardTitle>Yeni bir teklif alın</CardTitle>
          <CardDescription>
            Branşınızı seçin, anlık primi ve risk skorunuzu görün, paketleri karşılaştırın.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Link to="/portal/quotes/new">
            <Button>Teklif Sihirbazını Başlat</Button>
          </Link>
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <QuickLink to="/portal/quotes" title="Tekliflerim" description="Aldığınız teklifleri görüntüleyin ve yönetin." />
        <QuickLink to="/portal/policies" title="Poliçelerim" description="Aktif/geçmiş poliçeleriniz ve PDF belgeleri." />
        <QuickLink to="/portal/claims" title="Hasarlarım" description="Hasar bildirin ve süreç durumunu takip edin." />
        <QuickLink to="/portal/renewals" title="Yenilemeler" description="Süresi yaklaşan poliçeleriniz için yenileme teklifleri." />
        <QuickLink to="/portal/profile" title="Profilim" description="Bilgilerinizi ve araç/konut kayıtlarınızı yönetin." />
      </div>
    </div>
  );
}

function QuickLink({ to, title, description }: { to: string; title: string; description: string }) {
  return (
    <Link to={to} className="block">
      <Card className="h-full transition-colors hover:border-primary">
        <CardHeader>
          <CardTitle className="text-base">{title}</CardTitle>
          <CardDescription>{description}</CardDescription>
        </CardHeader>
      </Card>
    </Link>
  );
}

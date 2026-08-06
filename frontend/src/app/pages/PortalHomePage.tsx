import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { StatCard } from "@/features/dashboard/components/StatCard";
import { PolicyStatusBadge } from "@/features/policies/components/PolicyStatusBadge";
import { QuoteStatusBadge } from "@/features/quotes/components/QuoteStatusBadge";
import { useClaimList } from "@/features/claims/hooks/useClaims";
import { usePolicyList } from "@/features/policies/hooks/usePolicies";
import { useMyProfile } from "@/features/profile/hooks/useProfile";
import { useQuoteList } from "@/features/quotes/hooks/useQuotes";
import { useRenewalList } from "@/features/renewals/hooks/useRenewals";
import type { PolicyListItem } from "@/features/policies/types/policy.types";
import type { QuoteSummary } from "@/features/quotes/types/quote.types";
import {
  AlertTriangleIcon,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  EmptyState,
  FileTextIcon,
  RefreshIcon,
  ShieldCheckIcon,
  Skeleton,
  UserIcon,
} from "@/shared/components";
import {
  INSURANCE_BRANCH_LABELS,
  PolicyStatus,
  type InsuranceBranch,
} from "@/shared/types/insurance.types";
import { formatCurrency, formatDate } from "@/shared/utils/format";

const RECENT_LIMIT = 4;

/**
 * Müşteri portalı ana sayfası: yaşayan bir dashboard. Karşılama bandı (ad + özet + birincil
 * aksiyon), hızlı işlemler, durum kartları (gerçek `totalCount`'lardan) ve son teklif/poliçe akışları.
 * Tümü mevcut müşteri uçlarından kompoze edilir — yeni API yoktur; backend değişmedi.
 */
export default function PortalHomePage() {
  const { session } = useAuth();
  const profile = useMyProfile();

  // Kartlar hem sayaç (totalCount) hem son kayıtları verir → tek sorgu iki amaca hizmet eder.
  const quotes = useQuoteList({ page: 1, pageSize: RECENT_LIMIT });
  const activePolicies = usePolicyList({ page: 1, pageSize: RECENT_LIMIT, status: PolicyStatus.Active });
  const claims = useClaimList({ page: 1, pageSize: 1 });
  const renewals = useRenewalList({ page: 1, pageSize: 1 });

  const firstName = profile.data?.firstName ?? "";
  const displayName = firstName !== "" ? firstName : (session?.email?.split("@")[0] ?? "");

  const activeCount = activePolicies.data?.totalCount;
  const quoteCount = quotes.data?.totalCount;
  const claimCount = claims.data?.totalCount;
  const renewalCount = renewals.data?.totalCount;

  return (
    <div className="space-y-6">
      <WelcomeBanner
        name={displayName}
        loading={profile.isLoading}
        activeCount={activeCount}
        quoteCount={quoteCount}
        renewalCount={renewalCount}
      />

      <section aria-label="Hızlı işlemler" className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <QuickAction to="/portal/quotes/new" icon={<FileTextIcon />} title="Yeni Teklif" description="Anında prim al" primary />
        <QuickAction to="/portal/claims/new" icon={<AlertTriangleIcon />} title="Hasar Bildir" description="Süreci başlat" />
        <QuickAction to="/portal/policies" icon={<ShieldCheckIcon />} title="Poliçelerim" description="Belgeleri gör" />
        <QuickAction to="/portal/profile" icon={<UserIcon />} title="Profilim" description="Bilgi & kayıtlar" />
      </section>

      <section aria-label="Özet" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Link to="/portal/policies" className="block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <StatCard title="Aktif Poliçe" value={statValue(activeCount, activePolicies.isLoading, activePolicies.isError)} icon={<ShieldCheckIcon />} hint="Yürürlükteki poliçeleriniz" />
        </Link>
        <Link to="/portal/quotes" className="block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <StatCard title="Tekliflerim" value={statValue(quoteCount, quotes.isLoading, quotes.isError)} icon={<FileTextIcon />} hint="Oluşturduğunuz teklifler" />
        </Link>
        <Link to="/portal/claims" className="block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <StatCard title="Hasarlarım" value={statValue(claimCount, claims.isLoading, claims.isError)} icon={<AlertTriangleIcon />} hint="Bildirdiğiniz hasarlar" />
        </Link>
        <Link to="/portal/renewals" className="block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <StatCard title="Yenilemeler" value={statValue(renewalCount, renewals.isLoading, renewals.isError)} icon={<RefreshIcon />} hint="Bekleyen yenileme teklifleri" />
        </Link>
      </section>

      <section aria-label="Son işlemler" className="grid gap-4 lg:grid-cols-2">
        <RecentCard
          title="Son Teklifler"
          to="/portal/quotes"
          isLoading={quotes.isLoading}
          isError={quotes.isError}
          isEmpty={(quotes.data?.items.length ?? 0) === 0}
          emptyIcon={<FileTextIcon />}
          emptyTitle="Henüz teklifiniz yok"
          emptyDescription="Yeni bir teklif alarak paketleri karşılaştırın."
          emptyAction={
            <Link to="/portal/quotes/new">
              <Button size="sm">Yeni Teklif Al</Button>
            </Link>
          }
        >
          {quotes.data?.items.slice(0, RECENT_LIMIT).map((quote) => (
            <QuoteRow key={quote.id} quote={quote} />
          ))}
        </RecentCard>

        <RecentCard
          title="Son Poliçeler"
          to="/portal/policies"
          isLoading={activePolicies.isLoading}
          isError={activePolicies.isError}
          isEmpty={(activePolicies.data?.items.length ?? 0) === 0}
          emptyIcon={<ShieldCheckIcon />}
          emptyTitle="Aktif poliçeniz yok"
          emptyDescription="Teklifinizi satın aldığınızda poliçeniz burada görünür."
          emptyAction={
            <Link to="/portal/quotes/new">
              <Button size="sm" variant="outline">Teklif Al</Button>
            </Link>
          }
        >
          {activePolicies.data?.items.slice(0, RECENT_LIMIT).map((policy) => (
            <PolicyRow key={policy.id} policy={policy} />
          ))}
        </RecentCard>
      </section>
    </div>
  );
}

/** Karşılama bandı: selamlama + kişiselleştirilmiş özet + birincil aksiyon. */
function WelcomeBanner({
  name,
  loading,
  activeCount,
  quoteCount,
  renewalCount,
}: {
  name: string;
  loading: boolean;
  activeCount?: number;
  quoteCount?: number;
  renewalCount?: number;
}) {
  const summary = buildSummary(activeCount, quoteCount, renewalCount);
  return (
    <Card className="border-primary/30 bg-gradient-to-br from-accent/60 to-card">
      <CardContent className="flex flex-col gap-4 py-6 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-4">
          <span
            aria-hidden="true"
            className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-primary/15 text-lg font-semibold text-primary"
          >
            {getInitials(name)}
          </span>
          <div className="min-w-0">
            <h1 className="truncate text-xl font-bold tracking-tight sm:text-2xl">
              {getGreeting()}{name !== "" ? `, ${name}` : ""}
            </h1>
            {loading ? (
              <Skeleton className="mt-1 h-4 w-56" />
            ) : (
              <p className="text-sm text-muted-foreground">{summary}</p>
            )}
          </div>
        </div>
        <Link to="/portal/quotes/new" className="shrink-0">
          <Button size="lg" className="w-full sm:w-auto">Yeni Teklif Al</Button>
        </Link>
      </CardContent>
    </Card>
  );
}

/** Hızlı işlem kartı: ikon + başlık + kısa açıklama; tüm kart tıklanabilir. */
function QuickAction({
  to,
  icon,
  title,
  description,
  primary = false,
}: {
  to: string;
  icon: ReactNode;
  title: string;
  description: string;
  primary?: boolean;
}) {
  return (
    <Link to={to} className="block rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
      <Card className="group h-full p-4 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md">
        <span
          aria-hidden="true"
          className={
            primary
              ? "flex h-10 w-10 items-center justify-center rounded-lg bg-primary text-primary-foreground [&_svg]:h-5 [&_svg]:w-5"
              : "flex h-10 w-10 items-center justify-center rounded-lg bg-accent text-accent-foreground transition-colors group-hover:bg-primary group-hover:text-primary-foreground [&_svg]:h-5 [&_svg]:w-5"
          }
        >
          {icon}
        </span>
        <p className="mt-3 font-semibold">{title}</p>
        <p className="text-xs text-muted-foreground">{description}</p>
      </Card>
    </Link>
  );
}

/** Son işlemler kartı: başlık + "Tümü" bağlantısı; yükleme/boş/hata durumlarını yönetir. */
function RecentCard({
  title,
  to,
  isLoading,
  isError,
  isEmpty,
  emptyIcon,
  emptyTitle,
  emptyDescription,
  emptyAction,
  children,
}: {
  title: string;
  to: string;
  isLoading: boolean;
  isError: boolean;
  isEmpty: boolean;
  emptyIcon: ReactNode;
  emptyTitle: string;
  emptyDescription: string;
  emptyAction: ReactNode;
  children: ReactNode;
}) {
  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between space-y-0">
        <CardTitle className="text-base">{title}</CardTitle>
        <Link to={to} className="text-sm font-medium text-primary hover:underline">
          Tümü
        </Link>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="space-y-3" aria-hidden="true">
            {Array.from({ length: 3 }).map((_, index) => (
              <div key={index} className="flex items-center justify-between gap-4">
                <div className="min-w-0 flex-1 space-y-1.5">
                  <Skeleton className="h-3.5 w-2/5" />
                  <Skeleton className="h-3 w-3/5" />
                </div>
                <Skeleton className="h-5 w-16 rounded-full" />
              </div>
            ))}
          </div>
        ) : isError ? (
          <p className="py-6 text-center text-sm text-destructive">Kayıtlar alınamadı.</p>
        ) : isEmpty ? (
          <EmptyState className="py-8" icon={emptyIcon} title={emptyTitle} description={emptyDescription} action={emptyAction} />
        ) : (
          <ul className="divide-y divide-border">{children}</ul>
        )}
      </CardContent>
    </Card>
  );
}

function QuoteRow({ quote }: { quote: QuoteSummary }) {
  return (
    <li>
      <Link
        to={`/portal/quotes/${quote.id}`}
        className="-mx-2 flex items-center justify-between gap-4 rounded-md px-2 py-2.5 transition-colors hover:bg-accent/60"
      >
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{quote.productName}</p>
          <p className="text-xs text-muted-foreground">
            {INSURANCE_BRANCH_LABELS[quote.branch as InsuranceBranch]} · {formatCurrency(quote.totalPremium)} · {formatDate(quote.createdAt)}
          </p>
        </div>
        <QuoteStatusBadge status={quote.status} />
      </Link>
    </li>
  );
}

function PolicyRow({ policy }: { policy: PolicyListItem }) {
  return (
    <li>
      <Link
        to={`/portal/policies/${policy.id}`}
        className="-mx-2 flex items-center justify-between gap-4 rounded-md px-2 py-2.5 transition-colors hover:bg-accent/60"
      >
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{policy.productName}</p>
          <p className="text-xs text-muted-foreground">
            <span className="font-mono text-primary">{policy.policyNumber}</span> · {formatCurrency(policy.totalPremium)} · Bitiş {formatDate(policy.endDate)}
          </p>
        </div>
        <PolicyStatusBadge status={policy.status} />
      </Link>
    </li>
  );
}

/** Sayaç değeri: yüklenirken iskelet, hata olduğunda "—", aksi halde sayı. */
function statValue(count: number | undefined, isLoading: boolean, isError: boolean): ReactNode {
  if (isLoading) {
    return <Skeleton className="h-8 w-10" />;
  }
  if (isError || count === undefined) {
    return "—";
  }
  return count;
}

/** Gerçek verilerden kişiselleştirilmiş özet cümlesi (yalnızca dolu olanlar birleştirilir). */
function buildSummary(activeCount?: number, quoteCount?: number, renewalCount?: number): string {
  const parts: string[] = [];
  if ((activeCount ?? 0) > 0) parts.push(`${activeCount} aktif poliçe`);
  if ((quoteCount ?? 0) > 0) parts.push(`${quoteCount} teklif`);
  if ((renewalCount ?? 0) > 0) parts.push(`${renewalCount} bekleyen yenileme`);
  if (parts.length === 0) {
    return "Sigorta yolculuğunuza ilk teklifinizle başlayın.";
  }
  return `Özet: ${parts.join(" · ")}.`;
}

/** Saate göre selamlama (tr-TR). */
function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 6) return "İyi geceler";
  if (hour < 12) return "Günaydın";
  if (hour < 18) return "İyi günler";
  return "İyi akşamlar";
}

/** Ad-soyaddan (veya e-postadan) en fazla iki baş harf (tr-TR büyük harf). */
function getInitials(source: string): string {
  const words = source.split(/[\s@._-]+/).filter((word) => word.length > 0);
  const letters = words.slice(0, 2).map((word) => word[0]);
  return (letters.join("") || "?").toLocaleUpperCase("tr-TR");
}

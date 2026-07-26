import { useMemo, useState } from "react";
import {
  usePricingVersions,
  useCreatePricingVersion,
} from "@/features/pricing/hooks/usePricing";
import type {
  PricingBranchRate,
  PricingVersion,
} from "@/features/pricing/types/pricing.types";
import {
  Alert,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ChartIcon,
  Drawer,
  EmptyState,
  Input,
  Label,
  ShieldCheckIcon,
  Skeleton,
  Textarea,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import {
  INSURANCE_BRANCH_LABELS,
  InsuranceBranch,
} from "@/shared/types/insurance.types";
import { formatCurrency } from "@/shared/utils/format";

const BRANCHES = Object.values(InsuranceBranch) as InsuranceBranch[];

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "long",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

/** `<input type="datetime-local">` için yerel saat biçimi. */
function toLocalInputValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Yüzde değişim (önceki → yeni). Önceki yoksa/0 ise hesaplanamaz → null. */
function percentChange(previous: number | null | undefined, next: number): number | null {
  if (previous === null || previous === undefined || previous === 0) {
    return null;
  }
  return ((next - previous) / previous) * 100;
}

function formatPercent(value: number): string {
  const sign = value > 0 ? "+" : "";
  return `${sign}${value.toLocaleString("tr-TR", { maximumFractionDigits: 2 })}%`;
}

/**
 * Fiyatlandırma Yönetimi (ADR-048/049). Admin yürürlükteki tarifeyi tablo halinde görür, yeni bir
 * versiyon yayınlarken değişiklikleri özet + onay adımıyla gözden geçirir ve geçmişi inceler.
 * Versiyonlar değişmezdir → geçmişi düzenleme arayüzü YOKTUR; admin yanlışlıkla geçmiş teklifleri etkileyemez.
 */
export default function AdminPricingPage() {
  const { data, isLoading, isError, error } = usePricingVersions();
  const [detail, setDetail] = useState<PricingVersion | null>(null);

  const current = useMemo(() => data?.find((version) => version.isCurrent), [data]);
  const scheduled = useMemo(
    () => data?.filter((version) => version.isScheduled) ?? [],
    [data],
  );
  const nextVersionNumber = useMemo(
    () => (data?.reduce((max, version) => Math.max(max, version.versionNumber), 0) ?? 0) + 1,
    [data],
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Fiyatlandırma Yönetimi</h1>
        <p className="text-muted-foreground">
          Branş bazlı baz primleri yönetin. Bir değişiklik yayınladığınızda yalnızca{" "}
          <strong>o andan sonra oluşturulan teklifler</strong> yeni fiyatları kullanır; mevcut
          teklif ve poliçelerin primleri korunur.
        </p>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-56 w-full rounded-xl" />
          <Skeleton className="h-72 w-full rounded-xl" />
        </div>
      ) : isError ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : (
        <>
          <ActiveTariffCard current={current} scheduled={scheduled} />

          <NewVersionSection current={current} nextVersionNumber={nextVersionNumber} />

          <HowPricingWorks />

          <PricingHistory versions={data ?? []} onSelect={setDetail} />
        </>
      )}

      <VersionDetailDrawer version={detail} onClose={() => setDetail(null)} />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Yürürlükteki tarife                                                 */
/* ------------------------------------------------------------------ */

/** Yürürlükteki tarife — admin'in "şu an hangi fiyatlar geçerli" sorusunun tam cevabı (tablo). */
function ActiveTariffCard({
  current,
  scheduled,
}: {
  current: PricingVersion | undefined;
  scheduled: PricingVersion[];
}) {
  if (current === undefined) {
    return null;
  }

  return (
    <Card>
      <CardHeader className="flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <CardTitle>
              {current.isBaseline ? "Yerleşik Varsayılan Tarife" : `Aktif Tarife · v${current.versionNumber}`}
            </CardTitle>
            <Badge variant="success">Aktif</Badge>
          </div>
          <CardDescription>
            {current.isBaseline
              ? "Henüz özel bir tarife yayınlanmadı — yeni teklifler sistemin yerleşik baz primlerini kullanıyor."
              : `${dateTimeFormatter.format(new Date(current.effectiveFrom))} tarihinden itibaren geçerli`}
          </CardDescription>
          {!current.isBaseline && current.createdByName !== null && (
            <p className="text-sm text-muted-foreground">
              Yayınlayan: <span className="font-medium text-foreground">{current.createdByName}</span>
            </p>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left text-xs uppercase tracking-wide text-muted-foreground">
                <th className="py-2 pr-4 font-medium">Branş</th>
                <th className="py-2 pr-4 text-right font-medium">Güncel Baz Prim</th>
                <th className="py-2 pr-4 text-right font-medium">Önceki</th>
                <th className="py-2 pr-4 text-right font-medium">Değişim</th>
                <th className="py-2 text-right font-medium">Durum</th>
              </tr>
            </thead>
            <tbody>
              {BRANCHES.map((branch) => {
                const rate = current.rates.find((item) => item.branch === branch);
                if (rate === undefined) {
                  return null;
                }
                return (
                  <tr key={branch} className="border-b last:border-0">
                    <td className="py-2.5 pr-4 font-medium">{INSURANCE_BRANCH_LABELS[branch]}</td>
                    <td className="py-2.5 pr-4 text-right font-semibold tabular-nums">
                      {formatCurrency(rate.basePremium)}
                    </td>
                    <td className="py-2.5 pr-4 text-right tabular-nums text-muted-foreground">
                      {rate.previousBasePremium === null
                        ? "—"
                        : formatCurrency(rate.previousBasePremium)}
                    </td>
                    <td className="py-2.5 pr-4 text-right">
                      <ChangeIndicator previous={rate.previousBasePremium} next={rate.basePremium} />
                    </td>
                    <td className="py-2.5 text-right">
                      <Badge variant="success">Aktif</Badge>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {scheduled.length > 0 && (
          <Alert>
            <span className="font-medium">Planlanmış tarife:</span>{" "}
            {scheduled.length === 1
              ? `v${scheduled[0].versionNumber}, ${dateTimeFormatter.format(new Date(scheduled[0].effectiveFrom))} tarihinde otomatik yürürlüğe girecek.`
              : `${scheduled.length} tarife ileri tarihli olarak planlandı; geçerlilik tarihleri geldiğinde otomatik yürürlüğe girer.`}
          </Alert>
        )}
      </CardContent>
    </Card>
  );
}

/** Önceki değere göre artış/azalış göstergesi (renk + ok). */
function ChangeIndicator({
  previous,
  next,
}: {
  previous: number | null;
  next: number;
}) {
  const change = percentChange(previous, next);
  if (change === null) {
    return <span className="text-xs text-muted-foreground">Yeni</span>;
  }
  if (Math.abs(change) < 0.005) {
    return <span className="text-xs text-muted-foreground">Değişmedi</span>;
  }
  const up = change > 0;
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 text-xs font-medium tabular-nums",
        up ? "text-warning" : "text-success",
      )}
    >
      <span aria-hidden="true">{up ? "▲" : "▼"}</span>
      {formatPercent(change)}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* Yeni tarife yayınlama (rehberli akış + özet + onay)                 */
/* ------------------------------------------------------------------ */

interface DraftRate {
  branch: InsuranceBranch;
  previous: number | null;
  next: number;
  valid: boolean;
  changed: boolean;
}

/** Yeni versiyon akışı — mevcut değerlerle karşılaştırmalı giriş, canlı özet ve onay adımı. */
function NewVersionSection({
  current,
  nextVersionNumber,
}: {
  current: PricingVersion | undefined;
  nextVersionNumber: number;
}) {
  const createVersion = useCreatePricingVersion();
  const [effectiveFrom, setEffectiveFrom] = useState(() => toLocalInputValue(new Date()));
  const [note, setNote] = useState("");
  const [reviewOpen, setReviewOpen] = useState(false);
  const [values, setValues] = useState<Record<number, string>>(() =>
    Object.fromEntries(
      BRANCHES.map((branch) => [
        branch,
        String(current?.rates.find((rate) => rate.branch === branch)?.basePremium ?? ""),
      ]),
    ),
  );

  const draft: DraftRate[] = BRANCHES.map((branch) => {
    const previous = current?.rates.find((rate) => rate.branch === branch)?.basePremium ?? null;
    const next = Number(values[branch]);
    const valid = Number.isFinite(next) && next > 0;
    return { branch, previous, next, valid, changed: valid && previous !== null && next !== previous };
  });

  const allValid = draft.every((rate) => rate.valid);
  const changedCount = draft.filter((rate) => rate.changed).length;
  const unchangedCount = draft.filter((rate) => rate.valid && !rate.changed).length;

  function publish() {
    createVersion.mutate(
      {
        effectiveFrom: new Date(effectiveFrom).toISOString(),
        note: note.trim() === "" ? null : note.trim(),
        rates: draft.map((rate) => ({ branch: rate.branch, basePremium: rate.next })),
      },
      { onSuccess: () => setReviewOpen(false) },
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Yeni Tarife Yayınla</CardTitle>
        <CardDescription>
          Mevcut tarife değiştirilmez; girdiğiniz değerlerle <strong>yeni bir versiyon</strong>{" "}
          oluşturulur. Her branş için güncel fiyatı görerek yeni fiyatı belirleyin.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {draft.map((rate) => (
            <div key={rate.branch} className="space-y-1.5 rounded-lg border bg-muted/30 p-3">
              <Label htmlFor={`branch-${rate.branch}`}>{INSURANCE_BRANCH_LABELS[rate.branch]}</Label>
              <p className="text-xs text-muted-foreground">
                Mevcut:{" "}
                <span className="font-medium text-foreground tabular-nums">
                  {rate.previous === null ? "—" : formatCurrency(rate.previous)}
                </span>
              </p>
              <Input
                id={`branch-${rate.branch}`}
                type="number"
                min={1}
                step={100}
                inputMode="decimal"
                value={values[rate.branch] ?? ""}
                onChange={(event) =>
                  setValues((state) => ({ ...state, [rate.branch]: event.target.value }))
                }
              />
              <div className="min-h-[1.25rem]">
                {rate.changed && (
                  <span className="text-xs">
                    <ChangeIndicator previous={rate.previous} next={rate.next} />
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label htmlFor="effectiveFrom">Geçerlilik Başlangıcı</Label>
            <Input
              id="effectiveFrom"
              type="datetime-local"
              value={effectiveFrom}
              onChange={(event) => setEffectiveFrom(event.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              Geçmiş bir tarih seçilemez — fiyat değişiklikleri geriye dönük uygulanmaz.
            </p>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="note">Açıklama (opsiyonel)</Label>
            <Textarea
              id="note"
              rows={3}
              maxLength={300}
              placeholder="Ör. 2026 üçüncü çeyrek enflasyon güncellemesi"
              value={note}
              onChange={(event) => setNote(event.target.value)}
            />
          </div>
        </div>

        <Alert>
          <span className="font-medium">Bu değişiklik yalnızca yeni teklifleri etkiler.</span>{" "}
          Mevcut teklifler ve poliçeler, oluşturuldukları tarihte kullanılan tarifeyi korur — primleri
          değişmez.
        </Alert>

        {createVersion.isSuccess && !reviewOpen && (
          <Alert variant="success">
            Yeni tarife yayınlandı. Bundan sonra oluşturulacak teklifler bu fiyatları kullanacak.
          </Alert>
        )}

        <div className="flex items-center justify-between gap-3">
          <p className="text-sm text-muted-foreground">
            {changedCount === 0
              ? "Henüz bir fiyat değişikliği yok."
              : `${changedCount} branşta fiyat değişecek, ${unchangedCount} branş aynı kalacak.`}
          </p>
          <Button disabled={!allValid} onClick={() => setReviewOpen(true)}>
            Değişiklikleri Gözden Geçir
          </Button>
        </div>
      </CardContent>

      <ReviewDrawer
        open={reviewOpen}
        onClose={() => setReviewOpen(false)}
        draft={draft}
        changedCount={changedCount}
        unchangedCount={unchangedCount}
        nextVersionNumber={nextVersionNumber}
        effectiveFrom={effectiveFrom}
        note={note}
        isPending={createVersion.isPending}
        error={createVersion.isError ? createVersion.error : undefined}
        onConfirm={publish}
      />
    </Card>
  );
}

/** Yayın öncesi son gözden geçirme + onay. Admin ne yayınladığını net görmeden onaylayamaz. */
function ReviewDrawer({
  open,
  onClose,
  draft,
  changedCount,
  unchangedCount,
  nextVersionNumber,
  effectiveFrom,
  note,
  isPending,
  error,
  onConfirm,
}: {
  open: boolean;
  onClose: () => void;
  draft: DraftRate[];
  changedCount: number;
  unchangedCount: number;
  nextVersionNumber: number;
  effectiveFrom: string;
  note: string;
  isPending: boolean;
  error?: unknown;
  onConfirm: () => void;
}) {
  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={`Tarife v${nextVersionNumber} — Değişiklik Özeti`}
      description="Yayınlamadan önce değişiklikleri gözden geçirin."
    >
      <div className="space-y-5">
        <ul className="space-y-2">
          {draft.map((rate) => {
            const change = percentChange(rate.previous, rate.next);
            const changed = rate.changed;
            return (
              <li
                key={rate.branch}
                className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2 text-sm"
              >
                <span className="font-medium">{INSURANCE_BRANCH_LABELS[rate.branch]}</span>
                <span className="flex items-center gap-2 tabular-nums">
                  {changed ? (
                    <>
                      <span className="text-muted-foreground line-through">
                        {rate.previous === null ? "—" : formatCurrency(rate.previous)}
                      </span>
                      <span className="font-semibold">{formatCurrency(rate.next)}</span>
                      {change !== null && <ChangeIndicator previous={rate.previous} next={rate.next} />}
                    </>
                  ) : (
                    <>
                      <span className="font-medium">{formatCurrency(rate.next)}</span>
                      <span className="text-xs text-muted-foreground">değişmedi</span>
                    </>
                  )}
                </span>
              </li>
            );
          })}
        </ul>

        <dl className="space-y-1.5 rounded-lg bg-muted/40 p-3 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Yürürlük tarihi</dt>
            <dd className="font-medium">{dateTimeFormatter.format(new Date(effectiveFrom))}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Fiyatı değişen branş</dt>
            <dd className="font-medium">{changedCount}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Değişmeyen branş</dt>
            <dd className="font-medium">{unchangedCount}</dd>
          </div>
          {note.trim() !== "" && (
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground">Açıklama</dt>
              <dd className="text-right font-medium">{note.trim()}</dd>
            </div>
          )}
        </dl>

        <Alert>
          <span className="font-medium">Mevcut teklif ve poliçeler etkilenmeyecek.</span> Bu tarife
          yalnızca yayınlandıktan sonra oluşturulacak tekliflerde kullanılır.
        </Alert>

        {error !== undefined && (
          <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isPending}>
            Vazgeç
          </Button>
          <Button onClick={onConfirm} disabled={isPending}>
            {isPending ? "Yayınlanıyor…" : `v${nextVersionNumber} Tarifesini Yayınla`}
          </Button>
        </div>
      </div>
    </Drawer>
  );
}

/* ------------------------------------------------------------------ */
/* Fiyatlandırma nasıl çalışır? (salt-okunur — çarpanlar kod-sabittir) */
/* ------------------------------------------------------------------ */

// ADR-054: Yalnızca GERÇEKTEN uygulanan faktörler listelenir. "Hasarsızlık indirimi" sistemde
// türetilmediği için (ve yenilemede hasar geçmişi zaten ayrı bir çarpanla fiyatlandığı için) buradan
// çıkarılmıştır — admin'e çalışmayan bir kaldıraç varmış gibi gösterilmez.
const FACTOR_GROUPS: { title: string; factors: string[] }[] = [
  {
    title: "Kasko & Trafik",
    factors: [
      "Sürücü yaşı",
      "Araç yaşı",
      "Motor gücü",
      "İl risk katsayısı",
      "Kullanım amacı (müşteri beyanı: hususi/ticari/taksi)",
      "Hasarsızlık basamağı (Bonus-Malus — geçmişten türetilir, branş bazlı)",
    ],
  },
  {
    title: "Konut & DASK",
    factors: ["Bina yaşı", "Metrekare", "Deprem bölgesi (adresten türetilir)"],
  },
  {
    title: "Sağlık",
    factors: ["Yaş bandı", "Sigara kullanımı (müşteri beyanı)"],
  },
];

/**
 * Motorun gerçekte nasıl çalıştığını açıklar. Risk çarpanları KOD İÇİNDE SABİTTİR (aktüeryal kurallar);
 * admin bunları düzenlemez — bu yüzden sahte bir "çarpan yönetimi" arayüzü YOKTUR. Admin yalnızca baz
 * primi yönetir; nihai prim, bu sabit çarpanlarla hesaplanır.
 */
function HowPricingWorks() {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <ShieldCheckIcon className="h-5 w-5 text-muted-foreground" />
          <CardTitle>Fiyatlandırma Nasıl Çalışır?</CardTitle>
        </div>
        <CardDescription>
          Nihai prim, yönettiğiniz baz prim ile sabit risk çarpanlarının çarpımıdır.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="rounded-lg border bg-muted/30 p-4 text-sm">
          <p className="font-medium">
            Nihai Prim = <span className="text-primary">Baz Prim</span> × Risk Çarpanları
          </p>
          <p className="mt-1 text-muted-foreground">
            <span className="font-medium text-foreground">Baz prim</span> bu ekrandan yönetilir.{" "}
            <span className="font-medium text-foreground">Risk çarpanları</span> aktüeryal kurallardır;
            teklifin risk bilgilerine göre otomatik uygulanır ve buradan değiştirilmez.
          </p>
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          {FACTOR_GROUPS.map((group) => (
            <div key={group.title} className="rounded-lg border p-3">
              <p className="text-sm font-semibold">{group.title}</p>
              <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
                {group.factors.map((factor) => (
                  <li key={factor} className="flex items-center gap-2">
                    <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-muted-foreground/50" />
                    {factor}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

/* ------------------------------------------------------------------ */
/* Fiyatlandırma geçmişi                                               */
/* ------------------------------------------------------------------ */

/** Fiyatlandırma geçmişi — her satır tıklanınca detay drawer'ı açılır. Versiyonlar değişmezdir. */
function PricingHistory({
  versions,
  onSelect,
}: {
  versions: PricingVersion[];
  onSelect: (version: PricingVersion) => void;
}) {
  const publishedCount = versions.filter((version) => !version.isBaseline).length;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Fiyatlandırma Geçmişi</CardTitle>
        <CardDescription>
          Her değişiklik yeni bir versiyon olarak saklanır; eski versiyonlar hiçbir zaman değiştirilmez.
          Ayrıntı için bir satıra tıklayın.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {publishedCount === 0 ? (
          <EmptyState
            icon={<ChartIcon />}
            title="Henüz özel tarife yayınlanmadı"
            description="İlk tarifenizi yayınladığınızda değişiklik geçmişi burada listelenir. Şu an yerleşik varsayılan tarife geçerlidir."
          />
        ) : (
          <ul className="space-y-3">
            {versions.map((version) => (
              <li key={version.isBaseline ? "baseline" : version.id}>
                <button
                  type="button"
                  onClick={() => onSelect(version)}
                  className="flex w-full flex-wrap items-center justify-between gap-2 rounded-lg border p-4 text-left transition-colors hover:bg-accent/50"
                >
                  <div className="min-w-0 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">
                        {version.isBaseline ? "Yerleşik Varsayılan Tarife" : `v${version.versionNumber}`}
                      </span>
                      {version.isCurrent && <Badge variant="success">Aktif</Badge>}
                      {version.isScheduled && <Badge variant="warning">Planlandı</Badge>}
                      {version.isBaseline && !version.isCurrent && (
                        <Badge variant="secondary">Başlangıç</Badge>
                      )}
                    </div>
                    {version.note !== null && (
                      <p className="truncate text-sm text-muted-foreground">{version.note}</p>
                    )}
                    {!version.isBaseline && version.createdByName !== null && (
                      <p className="text-xs text-muted-foreground">
                        Yayınlayan:{" "}
                        <span className="font-medium text-foreground">{version.createdByName}</span>
                      </p>
                    )}
                  </div>
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {version.isBaseline
                      ? "Başlangıç tarifesi"
                      : `${dateTimeFormatter.format(new Date(version.effectiveFrom))} itibarıyla`}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

/** Tarife detayı — bu versiyonda ne değişti, branş bazında eski → yeni fiyat ve oran. */
function VersionDetailDrawer({
  version,
  onClose,
}: {
  version: PricingVersion | null;
  onClose: () => void;
}) {
  return (
    <Drawer
      open={version !== null}
      onClose={onClose}
      title={
        version === null
          ? ""
          : version.isBaseline
            ? "Yerleşik Varsayılan Tarife"
            : `Tarife v${version.versionNumber}`
      }
      description={
        version === null
          ? undefined
          : version.isBaseline
            ? "Sistemin kod içindeki başlangıç baz primleri."
            : `${dateTimeFormatter.format(new Date(version.effectiveFrom))} itibarıyla geçerli`
      }
    >
      {version !== null && (
        <div className="space-y-4">
          <div className="flex flex-wrap gap-2">
            {version.isCurrent && <Badge variant="success">Aktif</Badge>}
            {version.isScheduled && <Badge variant="warning">Planlandı</Badge>}
            {!version.isBaseline && version.createdByName !== null && (
              <Badge variant="secondary">Yayınlayan: {version.createdByName}</Badge>
            )}
          </div>

          {version.note !== null && (
            <p className="text-sm text-muted-foreground">{version.note}</p>
          )}

          <ul className="space-y-2">
            {version.rates.map((rate) => (
              <DetailRateRow key={rate.branch} rate={rate} />
            ))}
          </ul>
        </div>
      )}
    </Drawer>
  );
}

function DetailRateRow({ rate }: { rate: PricingBranchRate }) {
  const changed = rate.previousBasePremium !== null && rate.previousBasePremium !== rate.basePremium;
  return (
    <li className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2.5 text-sm">
      <span className="font-medium">{INSURANCE_BRANCH_LABELS[rate.branch]}</span>
      <span className="flex items-center gap-2 tabular-nums">
        {changed && (
          <span className="text-muted-foreground line-through">
            {formatCurrency(rate.previousBasePremium as number)}
          </span>
        )}
        <span className="font-semibold">{formatCurrency(rate.basePremium)}</span>
        <ChangeIndicator previous={rate.previousBasePremium} next={rate.basePremium} />
      </span>
    </li>
  );
}

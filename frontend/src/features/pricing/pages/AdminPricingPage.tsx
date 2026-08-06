import { useMemo, useState } from "react";
import { useRoles } from "@/features/auth/hooks/useRoles";
import {
  useActivatePricingVersion,
  useCreatePricingDraft,
  useDiscardPricingDraft,
  usePricingVersions,
  useUpdatePricingDraft,
} from "@/features/pricing/hooks/usePricing";
import {
  BAND_FACTOR_LABELS,
  PRICING_STATUS_BADGE_VARIANTS,
  PRICING_STATUS_LABELS,
  PricingVersionStatus,
  type BandKey,
  type PricingVersion,
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
  Drawer,
  Input,
  Label,
  Skeleton,
  Textarea,
} from "@/shared/components";
import { Combobox } from "@/shared/components/Combobox";
import { useCityCatalog } from "@/shared/hooks/useCityCatalog";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { cn } from "@/shared/lib/utils";
import {
  COVERAGE_PACKAGE_LABELS,
  CoveragePackage,
  INSURANCE_BRANCH_LABELS,
  InsuranceBranch,
} from "@/shared/types/insurance.types";
import { formatCurrency } from "@/shared/utils/format";

const BRANCHES = Object.values(InsuranceBranch) as InsuranceBranch[];
const PACKAGES = Object.values(CoveragePackage) as CoveragePackage[];
const BAND_KEYS = Object.keys(BAND_FACTOR_LABELS) as BandKey[];

// Faktör gruplarının panel düzeni (istenen 5 grup).
const VEHICLE_BANDS: BandKey[] = ["vehicleAgeFactors", "enginePowerFactors", "vehicleUsageFactors", "bonusMalusFactors"];
const PROPERTY_BANDS: BandKey[] = ["buildingAgeFactors", "squareMetersFactors", "earthquakeZoneFactors"];

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "long",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

function toLocalInputValue(iso: string | null): string {
  if (iso === null) return "";
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function percentChange(previous: number | null | undefined, next: number): number | null {
  if (previous === null || previous === undefined || previous === 0) return null;
  return ((next - previous) / previous) * 100;
}

function ChangeIndicator({ previous, next }: { previous: number | null; next: number }) {
  const change = percentChange(previous, next);
  if (change === null) return <span className="text-xs text-muted-foreground">Yeni</span>;
  if (Math.abs(change) < 0.005) return <span className="text-xs text-muted-foreground">—</span>;
  const up = change > 0;
  const sign = up ? "+" : "";
  return (
    <span className={cn("text-xs font-medium tabular-nums", up ? "text-warning" : "text-success")}>
      {up ? "▲" : "▼"} {sign}
      {change.toLocaleString("tr-TR", { maximumFractionDigits: 1 })}%
    </span>
  );
}

function PricingStatusBadge({ version }: { version: PricingVersion }) {
  if (version.isBaseline) {
    return <Badge variant="secondary" className="px-3 py-1 text-sm">Başlangıç</Badge>;
  }
  return (
    <Badge variant={PRICING_STATUS_BADGE_VARIANTS[version.status]} className="px-3 py-1 text-sm font-semibold">
      {PRICING_STATUS_LABELS[version.status]}
    </Badge>
  );
}

/**
 * Fiyatlandırma Yönetimi. Gerçek sigortacılıktaki tarife yaşam döngüsü: TASLAK hazırla → düzenle →
 * (değişiklik özetini gör) → AKTİFLEŞTİR. Aktifleştirmeden sonra yalnızca YENİ teklifler yeni tarifeyi kullanır;
 * mevcut teklif/poliçe/PDF/rapor/snapshot primleri asla değişmez. Admin düzenler; Personel yalnızca görüntüler.
 */
export default function AdminPricingPage() {
  const { isAdmin } = useRoles();
  const { data, isLoading, isError, error } = usePricingVersions();

  const active = useMemo(
    () => data?.find((version) => version.status === PricingVersionStatus.Active),
    [data],
  );
  const baseline = useMemo(() => data?.find((version) => version.isBaseline), [data]);
  const draft = useMemo(
    () => data?.find((version) => version.status === PricingVersionStatus.Draft),
    [data],
  );
  const activeOrBaseline = active ?? baseline;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Fiyatlandırma Yönetimi</h1>
        <p className="text-muted-foreground">
          Tarifeyi versiyonlayarak yönetin. Bir versiyonu <strong>aktifleştirdiğinizde</strong> yalnızca{" "}
          <strong>o andan sonra oluşturulan teklifler</strong> yeni fiyatları kullanır; mevcut teklif,
          poliçe ve raporların primleri korunur.
        </p>
      </div>

      {!isAdmin && (
        <Alert>
          <span className="font-medium">Görüntüleme modu.</span> Tarifeyi yalnızca yönetici (Admin)
          değiştirebilir; siz mevcut tarifeyi ve geçmişi görüntüleyebilirsiniz.
        </Alert>
      )}

      {isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-56 w-full rounded-xl" />
          <Skeleton className="h-72 w-full rounded-xl" />
        </div>
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : (
        <>
          {activeOrBaseline !== undefined && <ActiveTariffCard version={activeOrBaseline} />}
          {isAdmin && activeOrBaseline !== undefined && (
            <DraftSection draft={draft} comparisonBase={activeOrBaseline} />
          )}
          <PricingHistory versions={data} />
        </>
      )}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Aktif tarife künyesi + baz primler (önceki/yeni + %) + faktör özeti */
/* ------------------------------------------------------------------ */

function ActiveTariffCard({ version }: { version: PricingVersion }) {
  return (
    <Card>
      <CardHeader className="space-y-3">
        <div className="flex flex-wrap items-center gap-3">
          <CardTitle>
            {version.isBaseline
              ? "Yerleşik Varsayılan Tarife"
              : `${version.name ?? "Tarife"} · v${version.versionNumber}`}
          </CardTitle>
          <PricingStatusBadge version={version} />
        </div>
        <CardDescription>
          {version.isBaseline
            ? "Henüz bir tarife aktifleştirilmedi — yeni teklifler sistemin yerleşik değerlerini kullanıyor."
            : "Şu an yürürlükteki tarife. Aşağıdaki değerlerle yeni teklifler fiyatlandırılır."}
        </CardDescription>
        {!version.isBaseline && (
          <dl className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2 lg:grid-cols-4">
            <MetaItem label="Versiyon No" value={`v${version.versionNumber}`} />
            <MetaItem label="Durum" value={PRICING_STATUS_LABELS[version.status]} />
            <MetaItem label="Oluşturulma" value={dateTimeFormatter.format(new Date(version.createdAt))} />
            <MetaItem
              label="Aktifleştirme"
              value={
                version.activatedAt !== null
                  ? dateTimeFormatter.format(new Date(version.activatedAt))
                  : dateTimeFormatter.format(new Date(version.effectiveFrom))
              }
            />
            <MetaItem label="Geçerlilik Başlangıcı" value={dateTimeFormatter.format(new Date(version.effectiveFrom))} />
            <MetaItem
              label="Geçerlilik Bitişi"
              value={version.effectiveTo !== null ? dateTimeFormatter.format(new Date(version.effectiveTo)) : "Süresiz"}
            />
            {version.createdByName !== null && <MetaItem label="Oluşturan" value={version.createdByName} />}
            {version.note !== null && version.note !== "" && <MetaItem label="Açıklama" value={version.note} />}
          </dl>
        )}
      </CardHeader>
      <CardContent className="space-y-5">
        <div>
          <h3 className="mb-2 text-sm font-semibold">Baz Primler</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="py-2 pr-4 font-medium">Branş</th>
                  <th className="py-2 pr-4 text-right font-medium">Önceki</th>
                  <th className="py-2 pr-4 text-right font-medium">Güncel</th>
                  <th className="py-2 text-right font-medium">Değişim</th>
                </tr>
              </thead>
              <tbody>
                {BRANCHES.map((branch) => {
                  const rate = version.rates.find((item) => item.branch === branch);
                  if (rate === undefined) return null;
                  return (
                    <tr key={branch} className="border-b last:border-0">
                      <td className="py-2 pr-4 font-medium">{INSURANCE_BRANCH_LABELS[branch]}</td>
                      <td className="py-2 pr-4 text-right tabular-nums text-muted-foreground">
                        {rate.previousBasePremium === null ? "—" : formatCurrency(rate.previousBasePremium)}
                      </td>
                      <td className="py-2 pr-4 text-right font-semibold tabular-nums">
                        {formatCurrency(rate.basePremium)}
                      </td>
                      <td className="py-2 text-right">
                        <ChangeIndicator previous={rate.previousBasePremium} next={rate.basePremium} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
        <RuleSetSummary version={version} />
      </CardContent>
    </Card>
  );
}

function MetaItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col">
      <dt className="text-xs uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  );
}

function RuleSetSummary({ version }: { version: PricingVersion }) {
  const { ruleSet } = version;
  const renewalPercent = Math.round((1 - ruleSet.renewalDiscountFactor) * 100);
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <div className="rounded-lg border p-3">
        <p className="text-sm font-semibold">Paket Çarpanları</p>
        <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
          {ruleSet.packagePremiumFactors.map((factor) => (
            <li key={factor.package} className="flex justify-between">
              <span>{COVERAGE_PACKAGE_LABELS[factor.package]}</span>
              <span className="font-medium tabular-nums text-foreground">×{factor.premiumFactor}</span>
            </li>
          ))}
        </ul>
      </div>
      <div className="rounded-lg border p-3">
        <p className="text-sm font-semibold">Yenileme İndirimi</p>
        <p className="mt-2 text-2xl font-bold tabular-nums">{renewalPercent === 0 ? "Yok" : `%${renewalPercent}`}</p>
        <p className="text-xs text-muted-foreground">Yenileme tekliflerine uygulanır.</p>
      </div>
      <div className="rounded-lg border p-3">
        <p className="text-sm font-semibold">İl Risk Katsayıları</p>
        <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
          {ruleSet.cityRiskCoefficients.slice(0, 5).map((city) => (
            <li key={city.city} className="flex justify-between">
              <span>{city.city}</span>
              <span className="font-medium tabular-nums text-foreground">×{city.coefficient}</span>
            </li>
          ))}
          <li className="flex justify-between border-t pt-1">
            <span>Diğer (varsayılan)</span>
            <span className="font-medium tabular-nums text-foreground">×{ruleSet.defaultCityRiskCoefficient}</span>
          </li>
        </ul>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Taslak: oluştur (isimli) / düzenle (gruplu) / özet / aktifleştir    */
/* ------------------------------------------------------------------ */

function DraftSection({ draft, comparisonBase }: { draft: PricingVersion | undefined; comparisonBase: PricingVersion }) {
  const createDraft = useCreatePricingDraft();
  const [name, setName] = useState("");

  if (draft === undefined) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Yeni Tarife Versiyonu</CardTitle>
          <CardDescription>
            Yeni bir <strong>taslak</strong> oluşturun. Taslak, aktif tarifenin tüm değerleriyle başlar; üzerinde
            çalışırken canlı fiyatlar etkilenmez.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {createDraft.isError && <Alert variant="destructive">{getApiErrorMessages(createDraft.error)[0]}</Alert>}
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-72 space-y-1.5">
              <Label htmlFor="draft-name">Taslak Adı</Label>
              <Input
                id="draft-name"
                placeholder="Ör. 2026 Q3 Tarife Güncellemesi"
                value={name}
                onChange={(event) => setName(event.target.value)}
              />
            </div>
            <Button
              onClick={() => createDraft.mutate({ name: name.trim() })}
              disabled={name.trim() === "" || createDraft.isPending}
            >
              {createDraft.isPending ? "Oluşturuluyor…" : "Taslak Oluştur"}
            </Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <DraftEditor draft={draft} comparisonBase={comparisonBase} />;
}

interface DraftForm {
  name: string;
  effectiveFrom: string;
  effectiveTo: string;
  note: string;
  basePremiums: Record<number, string>;
  packageFactors: Record<number, string>;
  renewalDiscountPercent: string;
  defaultCityCoefficient: string;
  cities: { city: string; coefficient: string }[];
  bands: Record<BandKey, string[]>;
  smokerSurcharge: string;
}

function toForm(draft: PricingVersion): DraftForm {
  const bands = {} as Record<BandKey, string[]>;
  for (const key of BAND_KEYS) {
    bands[key] = draft.ruleSet[key].map((value) => String(value));
  }
  return {
    name: draft.name ?? `v${draft.versionNumber}`,
    effectiveFrom: toLocalInputValue(draft.effectiveFrom),
    effectiveTo: toLocalInputValue(draft.effectiveTo),
    note: draft.note ?? "",
    basePremiums: Object.fromEntries(
      BRANCHES.map((branch) => [branch, String(draft.rates.find((r) => r.branch === branch)?.basePremium ?? "")]),
    ),
    packageFactors: Object.fromEntries(
      PACKAGES.map((pkg) => [
        pkg,
        String(draft.ruleSet.packagePremiumFactors.find((f) => f.package === pkg)?.premiumFactor ?? ""),
      ]),
    ),
    renewalDiscountPercent: String(Math.round((1 - draft.ruleSet.renewalDiscountFactor) * 100)),
    defaultCityCoefficient: String(draft.ruleSet.defaultCityRiskCoefficient),
    cities: draft.ruleSet.cityRiskCoefficients.map((c) => ({ city: c.city, coefficient: String(c.coefficient) })),
    bands,
    smokerSurcharge: String(draft.ruleSet.smokerSurcharge),
  };
}

interface ChangeRow {
  label: string;
  from: string;
  to: string;
  fromValue: number | null;
  toValue: number;
}

function computeChanges(form: DraftForm, base: PricingVersion): ChangeRow[] {
  const rows: ChangeRow[] = [];
  const push = (label: string, prev: number | null, next: number, fmt: (v: number) => string) => {
    if (prev === null || prev !== next) {
      rows.push({ label, from: prev === null ? "—" : fmt(prev), to: fmt(next), fromValue: prev, toValue: next });
    }
  };
  const fx = (v: number) => `×${v}`;

  for (const branch of BRANCHES) {
    const prev = base.rates.find((r) => r.branch === branch)?.basePremium ?? null;
    const next = Number(form.basePremiums[branch]);
    if (prev !== null && next !== prev) push(`Baz Prim · ${INSURANCE_BRANCH_LABELS[branch]}`, prev, next, formatCurrency);
  }
  for (const pkg of PACKAGES) {
    const prev = base.ruleSet.packagePremiumFactors.find((f) => f.package === pkg)?.premiumFactor ?? null;
    const next = Number(form.packageFactors[pkg]);
    if (prev !== null && next !== prev) push(`Paket · ${COVERAGE_PACKAGE_LABELS[pkg]}`, prev, next, fx);
  }
  const nextRenewal = Number(form.renewalDiscountPercent);
  const prevRenewal = Math.round((1 - base.ruleSet.renewalDiscountFactor) * 100);
  if (nextRenewal !== prevRenewal) {
    rows.push({ label: "Yenileme İndirimi", from: `%${prevRenewal}`, to: `%${nextRenewal}`, fromValue: prevRenewal, toValue: nextRenewal });
  }
  const nextDefault = Number(form.defaultCityCoefficient);
  if (nextDefault !== base.ruleSet.defaultCityRiskCoefficient) {
    push("Varsayılan İl Katsayısı", base.ruleSet.defaultCityRiskCoefficient, nextDefault, fx);
  }
  // İl katsayıları
  const baseCities = new Map(base.ruleSet.cityRiskCoefficients.map((c) => [c.city.toLowerCase(), c]));
  const formKeys = new Set(form.cities.map((c) => c.city.trim().toLowerCase()));
  for (const city of form.cities) {
    const key = city.city.trim().toLowerCase();
    if (key === "") continue;
    const prev = baseCities.get(key);
    const next = Number(city.coefficient);
    if (prev === undefined) rows.push({ label: `İl · ${city.city.trim()}`, from: "—", to: fx(next), fromValue: null, toValue: next });
    else if (prev.coefficient !== next) push(`İl · ${city.city.trim()}`, prev.coefficient, next, fx);
  }
  for (const [key, prev] of baseCities) {
    if (!formKeys.has(key)) rows.push({ label: `İl · ${prev.city}`, from: fx(prev.coefficient), to: "kaldırıldı", fromValue: prev.coefficient, toValue: 0 });
  }
  // Bantlı faktörler
  for (const bandKey of BAND_KEYS) {
    const labels = BAND_FACTOR_LABELS[bandKey].labels;
    const prevArr = base.ruleSet[bandKey];
    form.bands[bandKey].forEach((raw, index) => {
      const next = Number(raw);
      const prev = prevArr[index] ?? null;
      if (prev !== null && next !== prev) {
        push(`${BAND_FACTOR_LABELS[bandKey].title} · ${labels[index] ?? index}`, prev, next, fx);
      }
    });
  }
  const nextSmoker = Number(form.smokerSurcharge);
  if (nextSmoker !== base.ruleSet.smokerSurcharge) push("Sigara Ek Primi", base.ruleSet.smokerSurcharge, nextSmoker, fx);

  return rows;
}

function DraftEditor({ draft, comparisonBase }: { draft: PricingVersion; comparisonBase: PricingVersion }) {
  const updateDraft = useUpdatePricingDraft();
  const activate = useActivatePricingVersion();
  const discard = useDiscardPricingDraft();
  const { data: catalog } = useCityCatalog();
  const [form, setForm] = useState<DraftForm>(() => toForm(draft));
  const [reviewOpen, setReviewOpen] = useState(false);
  const [addKey, setAddKey] = useState(0);

  const allCities = catalog?.cities.map((c) => c.name) ?? [];
  const selectedLower = new Set(form.cities.map((c) => c.city.trim().toLowerCase()));
  const availableCities = allCities.filter((name) => !selectedLower.has(name.toLowerCase()));

  const renewalFactor = 1 - Number(form.renewalDiscountPercent) / 100;
  const positive = (v: string) => Number(v) > 0;
  const valid =
    form.name.trim() !== "" &&
    form.effectiveFrom !== "" &&
    BRANCHES.every((b) => positive(form.basePremiums[b])) &&
    PACKAGES.every((p) => positive(form.packageFactors[p])) &&
    positive(form.defaultCityCoefficient) &&
    positive(form.smokerSurcharge) &&
    renewalFactor > 0 &&
    renewalFactor <= 1 &&
    form.cities.every((c) => c.city.trim() !== "" && positive(c.coefficient)) &&
    BAND_KEYS.every((k) => form.bands[k].every(positive));

  const setBand = (key: BandKey, index: number, value: string) =>
    setForm((state) => ({
      ...state,
      bands: { ...state.bands, [key]: state.bands[key].map((v, i) => (i === index ? value : v)) },
    }));

  function buildRequest() {
    const bands = {} as Record<BandKey, number[]>;
    for (const key of BAND_KEYS) bands[key] = form.bands[key].map(Number);
    return {
      name: form.name.trim(),
      effectiveFrom: new Date(form.effectiveFrom).toISOString(),
      effectiveTo: form.effectiveTo === "" ? null : new Date(form.effectiveTo).toISOString(),
      note: form.note.trim() === "" ? null : form.note.trim(),
      rates: BRANCHES.map((branch) => ({ branch, basePremium: Number(form.basePremiums[branch]) })),
      packagePremiumFactors: PACKAGES.map((pkg) => ({ package: pkg, premiumFactor: Number(form.packageFactors[pkg]) })),
      cityRiskCoefficients: form.cities.map((c) => ({ city: c.city.trim(), coefficient: Number(c.coefficient) })),
      defaultCityRiskCoefficient: Number(form.defaultCityCoefficient),
      renewalDiscountFactor: Number(renewalFactor.toFixed(4)),
      smokerSurcharge: Number(form.smokerSurcharge),
      ...bands,
    };
  }

  const changes = computeChanges(form, comparisonBase);

  // Customer Profile (ProfilePage) tasarım dilini birebir izler: her bölüm BAĞIMSIZ bir <Card> (CardHeader +
  // CardTitle/CardDescription + CardContent), kartlar `space-y-6` ile ayrılır. Hiçbir input/state/validasyon
  // değişmez — yalnızca mevcut alanlar ayrı kartlara dağıtıldı.
  return (
    <div className="space-y-6">
      {/* Versiyon Bilgileri */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <CardTitle>Versiyon Bilgileri</CardTitle>
            <PricingStatusBadge version={draft} />
          </div>
          <CardDescription>
            Değişiklikler canlı fiyatları etkilemez. Kaydedin, ardından değişiklik özetini görüp aktifleştirin.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <dl className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2 lg:grid-cols-4">
            <MetaItem label="Versiyon No" value={`v${draft.versionNumber}`} />
            <MetaItem label="Durum" value={PRICING_STATUS_LABELS[draft.status]} />
            <MetaItem label="Oluşturulma" value={dateTimeFormatter.format(new Date(draft.createdAt))} />
            <MetaItem
              label="Aktifleştirilme"
              value={draft.activatedAt !== null ? dateTimeFormatter.format(new Date(draft.activatedAt)) : "—"}
            />
          </dl>
          <section className="grid gap-3 sm:grid-cols-3">
            <div className="space-y-1.5">
              <Label htmlFor="edit-name">Taslak Adı</Label>
              <Input id="edit-name" value={form.name} onChange={(e) => setForm((s) => ({ ...s, name: e.target.value }))} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="edit-from">Geçerlilik Başlangıcı</Label>
              <Input
                id="edit-from"
                type="datetime-local"
                value={form.effectiveFrom}
                onChange={(e) => setForm((s) => ({ ...s, effectiveFrom: e.target.value }))}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="edit-to">Geçerlilik Bitişi (opsiyonel)</Label>
              <Input
                id="edit-to"
                type="datetime-local"
                value={form.effectiveTo}
                onChange={(e) => setForm((s) => ({ ...s, effectiveTo: e.target.value }))}
              />
            </div>
          </section>
          <section className="space-y-1.5">
            <Label htmlFor="draft-note">Açıklama (opsiyonel)</Label>
            <Textarea
              id="draft-note"
              rows={2}
              maxLength={300}
              value={form.note}
              onChange={(e) => setForm((s) => ({ ...s, note: e.target.value }))}
            />
          </section>
        </CardContent>
      </Card>

      {/* Ticari Ayarlar */}
      <Card>
        <CardHeader>
          <CardTitle>Ticari Ayarlar</CardTitle>
          <CardDescription>Baz primler, paket ve yenileme katsayıları.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-3">
            <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Baz Primler</h4>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {BRANCHES.map((branch) => (
                <NumberField
                  key={branch}
                  label={INSURANCE_BRANCH_LABELS[branch]}
                  value={form.basePremiums[branch] ?? ""}
                  step={100}
                  onChange={(v) => setForm((s) => ({ ...s, basePremiums: { ...s.basePremiums, [branch]: v } }))}
                />
              ))}
            </div>
          </div>
          <div className="space-y-3">
            <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Paket Katsayıları</h4>
            <div className="grid gap-3 sm:grid-cols-3">
              {PACKAGES.map((pkg) => (
                <NumberField
                  key={pkg}
                  label={COVERAGE_PACKAGE_LABELS[pkg]}
                  value={form.packageFactors[pkg] ?? ""}
                  step={0.05}
                  onChange={(v) => setForm((s) => ({ ...s, packageFactors: { ...s.packageFactors, [pkg]: v } }))}
                />
              ))}
            </div>
          </div>
          <div className="space-y-3">
            <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Yenileme Katsayıları</h4>
            <div className="grid gap-3 sm:grid-cols-2">
              <NumberField
                label="Yenileme İndirimi (%)"
                value={form.renewalDiscountPercent}
                step={1}
                hint="Yenileme tekliflerine uygulanır. 0 = indirim yok."
                onChange={(v) => setForm((s) => ({ ...s, renewalDiscountPercent: v }))}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Araç Fiyatlama Faktörleri */}
      <Card>
        <CardHeader>
          <CardTitle>Araç Fiyatlama Faktörleri</CardTitle>
          <CardDescription>Kasko / Trafik tekliflerinde araca göre uygulanan çarpanlar.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {VEHICLE_BANDS.map((key) => (
            <BandFactorEditor key={key} bandKey={key} values={form.bands[key]} onChange={setBand} />
          ))}
        </CardContent>
      </Card>

      {/* Sürücü Fiyatlama Faktörleri */}
      <Card>
        <CardHeader>
          <CardTitle>Sürücü Fiyatlama Faktörleri</CardTitle>
          <CardDescription>Kasko / Trafik tekliflerinde sürücüye göre uygulanan çarpanlar.</CardDescription>
        </CardHeader>
        <CardContent>
          <BandFactorEditor bandKey="driverAgeFactors" values={form.bands.driverAgeFactors} onChange={setBand} />
        </CardContent>
      </Card>

      {/* Konut Fiyatlama Faktörleri */}
      <Card>
        <CardHeader>
          <CardTitle>Konut Fiyatlama Faktörleri</CardTitle>
          <CardDescription>Konut / DASK tekliflerinde uygulanan çarpanlar.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {PROPERTY_BANDS.map((key) => (
            <BandFactorEditor key={key} bandKey={key} values={form.bands[key]} onChange={setBand} />
          ))}
        </CardContent>
      </Card>

      {/* Sağlık Fiyatlama Faktörleri */}
      <Card>
        <CardHeader>
          <CardTitle>Sağlık Fiyatlama Faktörleri</CardTitle>
          <CardDescription>Sağlık tekliflerinde uygulanan çarpanlar.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <BandFactorEditor bandKey="healthAgeFactors" values={form.bands.healthAgeFactors} onChange={setBand} />
          <div className="grid gap-3 sm:grid-cols-3">
            <NumberField
              label="Sigara Ek Prim Çarpanı"
              value={form.smokerSurcharge}
              step={0.05}
              hint="Kullanmıyor = 1.00 (sabit)."
              onChange={(v) => setForm((s) => ({ ...s, smokerSurcharge: v }))}
            />
          </div>
        </CardContent>
      </Card>

      {/* İl Katsayıları */}
      <Card>
        <CardHeader>
          <CardTitle>İl Katsayıları</CardTitle>
          <CardDescription>İl bazlı risk katsayıları ve varsayılan katsayı yönetimi.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <NumberField
              label="Varsayılan İl Risk Katsayısı"
              value={form.defaultCityCoefficient}
              step={0.05}
              hint="Aşağıdaki listede olmayan iller için."
              onChange={(v) => setForm((s) => ({ ...s, defaultCityCoefficient: v }))}
            />
          </div>
          <CityCoefficientEditor
            cities={form.cities}
            availableCities={availableCities}
            addKey={addKey}
            onAdd={(name) => {
              setForm((s) => ({ ...s, cities: [...s.cities, { city: name, coefficient: "1.00" }] }));
              setAddKey((k) => k + 1);
            }}
            onCoefficient={(index, value) =>
              setForm((s) => ({ ...s, cities: s.cities.map((c, i) => (i === index ? { ...c, coefficient: value } : c)) }))
            }
            onRemove={(index) => setForm((s) => ({ ...s, cities: s.cities.filter((_, i) => i !== index) }))}
          />
        </CardContent>
      </Card>

      {/* Aksiyonlar */}
      <Card>
        <CardContent className="space-y-4 pt-6">
          <Alert>
            <span className="font-medium">Aktifleştirene kadar canlı fiyatlar değişmez.</span> Aktifleştirdiğinizde
            yalnızca sonraki teklifler bu tarifeyi kullanır; mevcut teklif ve poliçeler korunur.
          </Alert>

          {updateDraft.isError && <Alert variant="destructive">{getApiErrorMessages(updateDraft.error)[0]}</Alert>}
          {discard.isError && <Alert variant="destructive">{getApiErrorMessages(discard.error)[0]}</Alert>}
          {updateDraft.isSuccess && !reviewOpen && !activate.isPending && (
            <Alert variant="success">Taslak kaydedildi.</Alert>
          )}

          <div className="flex flex-wrap items-center justify-between gap-2">
            <Button variant="ghost" onClick={() => discard.mutate(draft.id)} disabled={discard.isPending || activate.isPending}>
              {discard.isPending ? "İptal ediliyor…" : "Taslağı İptal Et"}
            </Button>
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                onClick={() => updateDraft.mutate({ id: draft.id, request: buildRequest() })}
                disabled={!valid || updateDraft.isPending}
              >
                {updateDraft.isPending ? "Kaydediliyor…" : "Taslağı Kaydet"}
              </Button>
              <Button
                onClick={() => updateDraft.mutate({ id: draft.id, request: buildRequest() }, { onSuccess: () => setReviewOpen(true) })}
                disabled={!valid || updateDraft.isPending}
              >
                Değişiklikleri Gözden Geçir
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <ReviewDrawer
        open={reviewOpen}
        onClose={() => setReviewOpen(false)}
        version={draft}
        changes={changes}
        isPending={activate.isPending}
        error={activate.isError ? activate.error : undefined}
        onConfirm={() => activate.mutate(draft.id, { onSuccess: () => setReviewOpen(false) })}
      />
    </div>
  );
}

function BandFactorEditor({
  bandKey,
  values,
  onChange,
}: {
  bandKey: BandKey;
  values: string[];
  onChange: (key: BandKey, index: number, value: string) => void;
}) {
  const { title, labels } = BAND_FACTOR_LABELS[bandKey];
  return (
    <div className="space-y-2">
      <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{title}</h4>
      <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-4">
        {values.map((value, index) => (
          <NumberField
            key={index}
            label={labels[index] ?? String(index)}
            value={value}
            step={0.05}
            onChange={(v) => onChange(bandKey, index, v)}
          />
        ))}
      </div>
    </div>
  );
}

function CityCoefficientEditor({
  cities,
  availableCities,
  addKey,
  onAdd,
  onCoefficient,
  onRemove,
}: {
  cities: { city: string; coefficient: string }[];
  availableCities: string[];
  addKey: number;
  onAdd: (name: string) => void;
  onCoefficient: (index: number, value: string) => void;
  onRemove: (index: number) => void;
}) {
  return (
    <div className="space-y-2">
      <h4 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">İl Risk Katsayıları</h4>
      {cities.length === 0 && (
        <p className="text-sm text-muted-foreground">Özel il katsayısı yok — tüm iller varsayılanı kullanır.</p>
      )}
      {cities.map((city, index) => (
        <div key={city.city} className="flex items-end gap-2">
          <div className="flex-1 space-y-1">
            <Label>İl</Label>
            <Input value={city.city} disabled />
          </div>
          <div className="w-32 space-y-1">
            <Label htmlFor={`coef-${index}`}>Katsayı</Label>
            <Input
              id={`coef-${index}`}
              type="number"
              step={0.05}
              min={0}
              value={city.coefficient}
              onChange={(e) => onCoefficient(index, e.target.value)}
            />
          </div>
          <Button type="button" variant="ghost" size="sm" onClick={() => onRemove(index)}>
            Sil
          </Button>
        </div>
      ))}
      <div className="max-w-sm space-y-1">
        <Label>İl Ekle</Label>
        <Combobox
          key={addKey}
          value=""
          options={availableCities}
          placeholder="Katalogdan il seçin veya arayın"
          emptyText="Tüm iller eklendi ya da bulunamadı."
          onChange={(picked) => {
            const name = picked.trim();
            if (name !== "") onAdd(name);
          }}
        />
        <p className="text-xs text-muted-foreground">
          İller il kataloğundan seçilir. Silinen bir il tekrar seçilebilir hâle gelir.
        </p>
      </div>
    </div>
  );
}

function ReviewDrawer({
  open,
  onClose,
  version,
  changes,
  isPending,
  error,
  onConfirm,
}: {
  open: boolean;
  onClose: () => void;
  version: PricingVersion;
  changes: ChangeRow[];
  isPending: boolean;
  error?: unknown;
  onConfirm: () => void;
}) {
  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={`${version.name ?? `v${version.versionNumber}`} — Değişiklik Özeti`}
      description="Aktifleştirmeden önce hangi değerlerin değişeceğini gözden geçirin."
    >
      <div className="space-y-5">
        {changes.length === 0 ? (
          <Alert>Aktif tarifeye göre bir değişiklik yok. Yine de bu versiyonu aktifleştirebilirsiniz.</Alert>
        ) : (
          <ul className="space-y-2">
            {changes.map((change) => (
              <li key={change.label} className="flex items-center justify-between gap-3 rounded-lg border px-3 py-2 text-sm">
                <span className="font-medium">{change.label}</span>
                <span className="flex items-center gap-2 tabular-nums">
                  <span className="text-muted-foreground line-through">{change.from}</span>
                  <span aria-hidden="true">↓</span>
                  <span className="font-semibold">{change.to}</span>
                  <ChangeIndicator previous={change.fromValue} next={change.toValue} />
                </span>
              </li>
            ))}
          </ul>
        )}
        <Alert>
          <span className="font-medium">Mevcut teklif ve poliçeler etkilenmeyecek.</span> Bu tarife yalnızca
          aktifleştirildikten sonra oluşturulacak tekliflerde kullanılır.
        </Alert>
        {error !== undefined && <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>}
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isPending}>
            Vazgeç
          </Button>
          <Button onClick={onConfirm} disabled={isPending}>
            {isPending ? "Aktifleştiriliyor…" : "Aktifleştir"}
          </Button>
        </div>
      </div>
    </Drawer>
  );
}

function NumberField({
  label,
  value,
  step,
  hint,
  onChange,
}: {
  label: string;
  value: string;
  step: number;
  hint?: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-1.5 rounded-lg border bg-muted/30 p-3">
      <Label className="text-xs">{label}</Label>
      <Input type="number" min={0} step={step} inputMode="decimal" value={value} onChange={(e) => onChange(e.target.value)} />
      {hint !== undefined && <p className="text-xs text-muted-foreground">{hint}</p>}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Fiyatlandırma geçmişi (salt-okunur)                                 */
/* ------------------------------------------------------------------ */

function PricingHistory({ versions }: { versions: PricingVersion[] }) {
  const publishedCount = versions.filter((v) => !v.isBaseline).length;
  return (
    <Card>
      <CardHeader>
        <CardTitle>Fiyatlandırma Geçmişi</CardTitle>
        <CardDescription>
          Her versiyon (taslak/aktif/arşiv) burada listelenir; aktif ve arşiv versiyonlar değiştirilemez —
          yalnızca taslak düzenlenebilir.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {publishedCount === 0 ? (
          <p className="py-8 text-center text-sm text-muted-foreground">
            Henüz bir tarife versiyonu yok. Şu an yerleşik varsayılan tarife geçerlidir.
          </p>
        ) : (
          <ul className="space-y-3">
            {versions.map((version) => (
              <li
                key={version.isBaseline ? "baseline" : version.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-lg border p-4"
              >
                <div className="min-w-0 space-y-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-semibold">
                      {version.isBaseline
                        ? "Yerleşik Varsayılan Tarife"
                        : `${version.name ?? "Tarife"} · v${version.versionNumber}`}
                    </span>
                    <PricingStatusBadge version={version} />
                  </div>
                  {version.note !== null && <p className="truncate text-sm text-muted-foreground">{version.note}</p>}
                  {!version.isBaseline && version.createdByName !== null && (
                    <p className="text-xs text-muted-foreground">
                      Oluşturan: <span className="font-medium text-foreground">{version.createdByName}</span>
                    </p>
                  )}
                </div>
                <span className="shrink-0 text-xs text-muted-foreground">
                  {version.isBaseline
                    ? "Başlangıç tarifesi"
                    : version.activatedAt !== null
                      ? `Aktif: ${dateTimeFormatter.format(new Date(version.activatedAt))}`
                      : `Oluşturuldu: ${dateTimeFormatter.format(new Date(version.createdAt))}`}
                </span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

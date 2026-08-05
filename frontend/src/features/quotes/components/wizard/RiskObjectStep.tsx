import { useState } from "react";
import { PropertyForm } from "@/features/profile/components/PropertyForm";
import { VehicleForm } from "@/features/profile/components/VehicleForm";
import { useAddProperty, useAddVehicle } from "@/features/profile/hooks/useProfile";
import type { Property, Vehicle } from "@/features/profile/types/profile.types";
import { InsuredPersonForm } from "@/features/quotes/components/wizard/InsuredPersonForm";
import { resolveRelationship } from "@/features/quotes/types/insuredPerson.schemas";
import type { InsuredPersonRequest } from "@/features/quotes/types/quote.types";
import { Alert, Button, Card, CardContent } from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { branchRiskKind, type InsuranceBranch } from "@/shared/types/insurance.types";

interface RiskObjectStepProps {
  branch: InsuranceBranch;
  vehicles: Vehicle[];
  properties: Property[];
  /** Acente destekli akışta hedef müşteri; null = self-servis (araç/konut oturum sahibine eklenir). */
  customerId: string | null;
  selectedId: string | null;
  onSelect: (id: string | null) => void;
  /** Sağlıkta "başkası adına" sigortalı beyanı (ADR-041); kendim için null. */
  insuredPerson: InsuredPersonRequest | null;
  onInsuredPersonChange: (insured: InsuredPersonRequest | null) => void;
  /** Sağlıkta sigara kullanım beyanı (ADR-054); null = henüz beyan edilmedi (varsayılan yok). */
  isSmoker: boolean | null;
  onIsSmokerChange: (value: boolean) => void;
  onBack: () => void;
  onNext: () => void;
}

/**
 * Sihirbaz 2. adım: risk objesi seçimi. Kasko/Trafik → mevcut araçtan seç veya inline ekle;
 * Konut/DASK → konut; Sağlık → risk objesi gerekmez (doğrudan devam). Inline ekleme, profil
 * feature'ının form + mutation'larını yeniden kullanır (DRY); eklenen obje otomatik seçilir.
 */
export function RiskObjectStep({
  branch,
  vehicles,
  properties,
  customerId,
  selectedId,
  onSelect,
  insuredPerson,
  onInsuredPersonChange,
  isSmoker,
  onIsSmokerChange,
  onBack,
  onNext,
}: RiskObjectStepProps) {
  const kind = branchRiskKind(branch);
  const [adding, setAdding] = useState(false);
  // Sağlık: "Kendim için" (self) / "Başkası adına" (other) — gerçek sigortacılık akışı (ADR-041).
  const [healthMode, setHealthMode] = useState<"self" | "other">(
    insuredPerson === null ? "self" : "other",
  );
  // customerId dolu → araç/konut, seçili müşteri adına eklenir (acente destekli); null → oturum sahibine.
  const addVehicle = useAddVehicle(customerId ?? undefined);
  const addProperty = useAddProperty(customerId ?? undefined);

  // ADR-054: Sağlıkta sigara beyanı ZORUNLUDUR — beyan alınmadan ilerlenemez (varsayılan atanmaz).
  const canProceed =
    kind === "none"
      ? (healthMode === "self" || insuredPerson !== null) && isSmoker !== null
      : selectedId !== null;

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-xl font-semibold">Risk bilgileri</h2>
        <p className="text-muted-foreground">
          {kind === "vehicle" && "Teklif almak istediğiniz aracı seçin."}
          {kind === "property" && "Teklif almak istediğiniz konutu seçin."}
          {kind === "none" && "Poliçeyi kimin için oluşturacağınızı seçin."}
        </p>
      </div>

      {kind === "none" && (
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-2">
            <SelectableRow
              selected={healthMode === "self"}
              onClick={() => {
                setHealthMode("self");
                onInsuredPersonChange(null);
              }}
              title="Kendim için"
              subtitle="Sigortalı sizsiniz; prim profil bilgilerinize göre hesaplanır."
            />
            <SelectableRow
              selected={healthMode === "other"}
              onClick={() => setHealthMode("other")}
              title="Başkası adına"
              subtitle="Eş, çocuk veya bir yakınınız için poliçe oluşturun."
            />
          </div>

          {healthMode === "self" && (
            <Alert>Sağlık sigortası teklifiniz doğrudan sizin profil bilgilerinize göre hesaplanır.</Alert>
          )}

          {healthMode === "other" && insuredPerson !== null && (
            <Card>
              <CardContent className="flex items-center justify-between py-4">
                <div>
                  <p className="font-medium">
                    {insuredPerson.firstName} {insuredPerson.lastName}{" "}
                    <span className="text-muted-foreground">({insuredPerson.relationship})</span>
                  </p>
                  <p className="text-sm text-muted-foreground">
                    Doğum tarihi {insuredPerson.birthDate} · {insuredPerson.phoneNumber}
                  </p>
                </div>
                <Button size="sm" variant="ghost" onClick={() => onInsuredPersonChange(null)}>
                  Değiştir
                </Button>
              </CardContent>
            </Card>
          )}

          {healthMode === "other" && insuredPerson === null && (
            <Card>
              <CardContent className="space-y-3 pt-6">
                <p className="text-sm text-muted-foreground">
                  Sigortalının bilgilerini girin. Gizlilik gereği sistemdeki diğer müşteriler
                  aranamaz; bilgiler sizin beyanınızdır.
                </p>
                <InsuredPersonForm
                  submitLabel="Sigortalıyı Kaydet"
                  onSubmit={(values) =>
                    onInsuredPersonChange({
                      firstName: values.firstName,
                      lastName: values.lastName,
                      tckn: values.tckn,
                      birthDate: values.birthDate,
                      phoneNumber: values.phoneNumber,
                      // "Diğer" seçildiyse açıklama, yakınlık derecesi olarak gönderilir (ADR-042).
                      relationship: resolveRelationship(values),
                    })
                  }
                />
              </CardContent>
            </Card>
          )}

          {/*
            ADR-054: Sigara beyanı fiyatı doğrudan etkiler (×1,25). Önceden hiç sorulmadan "içmiyor"
            varsayılıyordu. Varsayılan seçim YOKTUR; beyan alınmadan devam edilemez. Sağlık verisinde
            veri minimizasyonu: yalnızca bu tek soru sorulur (tanı/tedavi bilgisi istenmez).
          */}
          <Card>
            <CardContent className="space-y-3 pt-6">
              <div>
                <p className="font-medium">
                  {healthMode === "self"
                    ? "Sigara kullanıyor musunuz?"
                    : "Sigortalı sigara kullanıyor mu?"}
                </p>
                <p className="text-sm text-muted-foreground">
                  Prim hesabını etkiler. Beyanınız yalnızca fiyatlandırma için kullanılır.
                </p>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <SelectableRow
                  selected={isSmoker === true}
                  onClick={() => onIsSmokerChange(true)}
                  title="Evet"
                  subtitle="Sigara kullanıyorum/kullanıyor."
                />
                <SelectableRow
                  selected={isSmoker === false}
                  onClick={() => onIsSmokerChange(false)}
                  title="Hayır"
                  subtitle="Sigara kullanmıyorum/kullanmıyor."
                />
              </div>
              {isSmoker === null && (
                <p className="text-xs text-muted-foreground">
                  Devam etmek için bu beyanı yapmanız gerekir.
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {kind === "vehicle" && (
        <div className="space-y-3">
          {vehicles.map((vehicle) => (
            <SelectableRow
              key={vehicle.id}
              selected={selectedId === vehicle.id}
              onClick={() => onSelect(vehicle.id)}
              title={`${vehicle.brand} ${vehicle.model} (${vehicle.manufactureYear})`}
              subtitle={`${vehicle.plateNumber} · ${vehicle.enginePowerHp} HP`}
            />
          ))}
          {vehicles.length === 0 && !adding && (
            <p className="text-sm text-muted-foreground">
              Kayıtlı aracınız yok. Devam etmek için bir araç ekleyin.
            </p>
          )}
          {adding ? (
            <Card>
              <CardContent className="pt-6">
                <VehicleForm
                  submitLabel="Ekle ve Seç"
                  isPending={addVehicle.isPending}
                  error={addVehicle.isError ? addVehicle.error : undefined}
                  onCancel={() => {
                    setAdding(false);
                    addVehicle.reset();
                  }}
                  onSubmit={(values) =>
                    addVehicle.mutate(values, {
                      onSuccess: (created) => {
                        onSelect(created.id);
                        setAdding(false);
                      },
                    })
                  }
                />
              </CardContent>
            </Card>
          ) : (
            <Button variant="outline" size="sm" onClick={() => setAdding(true)}>
              Yeni araç ekle
            </Button>
          )}
        </div>
      )}

      {kind === "property" && (
        <div className="space-y-3">
          {properties.map((property) => (
            <SelectableRow
              key={property.id}
              selected={selectedId === property.id}
              onClick={() => onSelect(property.id)}
              title={`${property.address.city} / ${property.address.district}`}
              subtitle={`${property.address.neighborhood} · ${property.squareMeters} m² · ${property.earthquakeZone}. deprem bölgesi (otomatik)`}
            />
          ))}
          {properties.length === 0 && !adding && (
            <p className="text-sm text-muted-foreground">
              Kayıtlı konutunuz yok. Devam etmek için bir konut ekleyin.
            </p>
          )}
          {adding ? (
            <Card>
              <CardContent className="pt-6">
                <PropertyForm
                  submitLabel="Ekle ve Seç"
                  isPending={addProperty.isPending}
                  error={addProperty.isError ? addProperty.error : undefined}
                  onCancel={() => {
                    setAdding(false);
                    addProperty.reset();
                  }}
                  onSubmit={(values) =>
                    addProperty.mutate(values, {
                      onSuccess: (created) => {
                        onSelect(created.id);
                        setAdding(false);
                      },
                    })
                  }
                />
              </CardContent>
            </Card>
          ) : (
            <Button variant="outline" size="sm" onClick={() => setAdding(true)}>
              Yeni konut ekle
            </Button>
          )}
        </div>
      )}

      <div className="flex gap-3 pt-2">
        <Button variant="outline" onClick={onBack}>
          Geri
        </Button>
        <Button onClick={onNext} disabled={!canProceed}>
          Devam
        </Button>
      </div>
    </div>
  );
}

function SelectableRow({
  selected,
  onClick,
  title,
  subtitle,
}: {
  selected: boolean;
  onClick: () => void;
  title: string;
  subtitle: string;
}) {
  return (
    <button type="button" onClick={onClick} className="block w-full text-left">
      <Card
        className={cn(
          "transition-colors hover:border-primary",
          selected && "border-primary ring-2 ring-primary",
        )}
      >
        <CardContent className="py-4">
          <p className="font-medium">{title}</p>
          <p className="text-sm text-muted-foreground">{subtitle}</p>
        </CardContent>
      </Card>
    </button>
  );
}

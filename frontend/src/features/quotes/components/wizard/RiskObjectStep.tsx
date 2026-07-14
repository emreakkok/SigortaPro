import { useState } from "react";
import { PropertyForm } from "@/features/profile/components/PropertyForm";
import { VehicleForm } from "@/features/profile/components/VehicleForm";
import { useAddProperty, useAddVehicle } from "@/features/profile/hooks/useProfile";
import type { Property, Vehicle } from "@/features/profile/types/profile.types";
import { Alert, Button, Card, CardContent } from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { branchRiskKind, type InsuranceBranch } from "@/shared/types/insurance.types";

interface RiskObjectStepProps {
  branch: InsuranceBranch;
  vehicles: Vehicle[];
  properties: Property[];
  selectedId: string | null;
  onSelect: (id: string | null) => void;
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
  selectedId,
  onSelect,
  onBack,
  onNext,
}: RiskObjectStepProps) {
  const kind = branchRiskKind(branch);
  const [adding, setAdding] = useState(false);
  const addVehicle = useAddVehicle();
  const addProperty = useAddProperty();

  const canProceed = kind === "none" || selectedId !== null;

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-xl font-semibold">Risk bilgileri</h2>
        <p className="text-muted-foreground">
          {kind === "vehicle" && "Teklif almak istediğiniz aracı seçin."}
          {kind === "property" && "Teklif almak istediğiniz konutu seçin."}
          {kind === "none" && "Sağlık sigortası için risk objesi gerekmez."}
        </p>
      </div>

      {kind === "none" && (
        <Alert>Sağlık sigortası teklifiniz doğrudan sizin profil bilgilerinize göre hesaplanır.</Alert>
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
              subtitle={`${property.address.neighborhood} · ${property.squareMeters} m² · ${property.earthquakeZone}. deprem bölgesi`}
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

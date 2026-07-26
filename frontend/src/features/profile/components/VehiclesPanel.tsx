import { useState } from "react";
import { VehicleForm } from "@/features/profile/components/VehicleForm";
import { useAddVehicle, useUpdateVehicle } from "@/features/profile/hooks/useProfile";
import type { Vehicle } from "@/features/profile/types/profile.types";
import { Button, Card, CardContent, EmptyState, FileTextIcon } from "@/shared/components";

/** Tek bir aracın düzenleme satırı; kendi güncelleme mutation'ını (id'ye bağlı) yönetir. */
function VehicleEditRow({ vehicle, onClose }: { vehicle: Vehicle; onClose: () => void }) {
  const updateMutation = useUpdateVehicle(vehicle.id);
  return (
    <Card>
      <CardContent className="pt-6">
        <VehicleForm
          // Beyanı olmayan (eski) araçta alan boş açılır → kullanıcı bilinçli seçim yapar.
          defaultValues={{ ...vehicle, usagePurpose: vehicle.usagePurpose ?? undefined }}
          submitLabel="Güncelle"
          isPending={updateMutation.isPending}
          error={updateMutation.isError ? updateMutation.error : undefined}
          onCancel={onClose}
          onSubmit={(values) => updateMutation.mutate(values, { onSuccess: onClose })}
        />
      </CardContent>
    </Card>
  );
}

/** Araç listesi + inline ekleme/düzenleme (Kasko/Trafik risk objeleri). */
export function VehiclesPanel({ vehicles }: { vehicles: Vehicle[] }) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const addMutation = useAddVehicle();

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Araçlarım</h3>
        {!adding && (
          <Button size="sm" variant="outline" onClick={() => setAdding(true)}>
            Araç Ekle
          </Button>
        )}
      </div>

      {adding && (
        <Card>
          <CardContent className="pt-6">
            <VehicleForm
              submitLabel="Ekle"
              isPending={addMutation.isPending}
              error={addMutation.isError ? addMutation.error : undefined}
              onCancel={() => {
                setAdding(false);
                addMutation.reset();
              }}
              onSubmit={(values) =>
                addMutation.mutate(values, { onSuccess: () => setAdding(false) })
              }
            />
          </CardContent>
        </Card>
      )}

      {vehicles.length === 0 && !adding && (
        <Card className="border-dashed">
          <EmptyState
            icon={<FileTextIcon />}
            title="Henüz araç eklemediniz"
            description="Kasko ve Trafik teklifleri için aracınızı ekleyin; teklif sihirbazında hazır seçilir."
            action={
              <Button size="sm" onClick={() => setAdding(true)}>
                Araç Ekle
              </Button>
            }
          />
        </Card>
      )}

      <div className="space-y-3">
        {vehicles.map((vehicle) =>
          editingId === vehicle.id ? (
            <VehicleEditRow
              key={vehicle.id}
              vehicle={vehicle}
              onClose={() => setEditingId(null)}
            />
          ) : (
            <Card key={vehicle.id}>
              <CardContent className="flex items-center justify-between py-4">
                <div>
                  <p className="font-medium">
                    {vehicle.brand} {vehicle.model}{" "}
                    <span className="text-muted-foreground">({vehicle.manufactureYear})</span>
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {vehicle.plateNumber} · {vehicle.enginePowerHp} HP
                  </p>
                </div>
                <Button size="sm" variant="ghost" onClick={() => setEditingId(vehicle.id)}>
                  Düzenle
                </Button>
              </CardContent>
            </Card>
          ),
        )}
      </div>
    </div>
  );
}

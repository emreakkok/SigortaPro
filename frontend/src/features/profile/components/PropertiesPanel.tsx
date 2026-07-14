import { useState } from "react";
import { PropertyForm } from "@/features/profile/components/PropertyForm";
import { useAddProperty } from "@/features/profile/hooks/useProfile";
import type { Property } from "@/features/profile/types/profile.types";
import { Button, Card, CardContent } from "@/shared/components";

/** Konut listesi + inline ekleme (Konut/DASK risk objeleri). Backend güncelleme ucu yok → yalnızca ekleme. */
export function PropertiesPanel({ properties }: { properties: Property[] }) {
  const [adding, setAdding] = useState(false);
  const addMutation = useAddProperty();

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Konutlarım</h3>
        {!adding && (
          <Button size="sm" variant="outline" onClick={() => setAdding(true)}>
            Konut Ekle
          </Button>
        )}
      </div>

      {adding && (
        <Card>
          <CardContent className="pt-6">
            <PropertyForm
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

      {properties.length === 0 && !adding && (
        <p className="text-sm text-muted-foreground">Henüz konut eklemediniz.</p>
      )}

      <div className="space-y-3">
        {properties.map((property) => (
          <Card key={property.id}>
            <CardContent className="py-4">
              <p className="font-medium">
                {property.address.city} / {property.address.district}
              </p>
              <p className="text-sm text-muted-foreground">
                {property.address.neighborhood} · {property.squareMeters} m² · Bina yaşı{" "}
                {property.buildingAge} · {property.earthquakeZone}. deprem bölgesi
              </p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

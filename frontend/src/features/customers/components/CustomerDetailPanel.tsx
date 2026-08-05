import { Link } from "react-router-dom";
import { useCustomer } from "@/features/customers/hooks/useCustomers";
import { Alert, Button, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import { formatDateOnly } from "@/shared/utils/format";

/** Detay çekmecesinde tek satırlık künye kalemi. */
function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-4 py-2 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="text-right font-medium">{value}</span>
    </div>
  );
}

/** Müşteri detay çekmecesi içeriği: künye + adres + risk objeleri (araç/konut). */
export function CustomerDetailPanel({ customerId }: { customerId: string }) {
  const { data, isLoading, isError, error } = useCustomer(customerId);

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  if (isError || data === undefined) {
    return <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>;
  }

  return (
    <div className="space-y-6">
      {/* Acente destekli teklif: personel bu müşteri adına ortak teklif sihirbazını başlatır (gerçek akış:
          telefonla arayan müşteri). Teklif sahibi müşteri olur; onay/ödeme müşteriye aittir. */}
      <Link to={`/admin/customers/${data.id}/quotes/new`} className="block">
        <Button className="w-full">Müşteri Adına Teklif Oluştur</Button>
      </Link>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">Kimlik & İletişim</h3>
        <div className="divide-y divide-border">
          <Row label="Ad Soyad" value={`${data.firstName} ${data.lastName}`} />
          <Row label="TCKN" value={data.maskedTckn} />
          <Row label="Doğum Tarihi" value={formatDateOnly(data.birthDate)} />
          <Row label="Telefon" value={data.phoneNumber} />
          <Row label="E-posta" value={data.email ?? "—"} />
          <Row
            label="Adres"
            value={`${data.address.neighborhood}, ${data.address.district} / ${data.address.city} ${data.address.postalCode}`}
          />
        </div>
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">
          Araçlar ({data.vehicles.length})
        </h3>
        {data.vehicles.length === 0 ? (
          <p className="py-2 text-sm text-muted-foreground">Kayıtlı araç yok.</p>
        ) : (
          <ul className="divide-y divide-border text-sm">
            {data.vehicles.map((vehicle) => (
              <li key={vehicle.id} className="py-2">
                <p className="font-mono font-medium">{vehicle.plateNumber}</p>
                <p className="text-muted-foreground">
                  {vehicle.brand} {vehicle.model} · {vehicle.manufactureYear} ·{" "}
                  {vehicle.enginePowerHp} HP
                </p>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section>
        <h3 className="mb-1 text-sm font-semibold text-muted-foreground">
          Konutlar ({data.properties.length})
        </h3>
        {data.properties.length === 0 ? (
          <p className="py-2 text-sm text-muted-foreground">Kayıtlı konut yok.</p>
        ) : (
          <ul className="divide-y divide-border text-sm">
            {data.properties.map((property) => (
              <li key={property.id} className="py-2">
                <p className="font-medium">
                  {property.address.district} / {property.address.city}
                </p>
                <p className="text-muted-foreground">
                  {property.squareMeters} m² · {property.buildingAge} yaş · Deprem bölgesi (otomatik){" "}
                  {property.earthquakeZone}
                </p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

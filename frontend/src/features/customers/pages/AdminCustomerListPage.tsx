import { useState } from "react";
import { CustomerDetailPanel } from "@/features/customers/components/CustomerDetailPanel";
import { useCustomerList } from "@/features/customers/hooks/useCustomers";
import {
  Alert,
  Card,
  CardContent,
  Drawer,
  Input,
  Label,
  EmptyState,
  PageSizeSelector,
  Pagination,
  SkeletonRows,
  UsersIcon,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";
import { formatDate } from "@/shared/utils/format";

/** Müşteri yönetimi: arama (ad/soyad/TCKN/e-posta/telefon) + il filtresi + tablo + detay çekmecesi. */
export default function AdminCustomerListPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [searchTerm, setSearchTerm] = useState("");
  const [city, setCity] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const debouncedSearch = useDebounce(searchTerm);
  const debouncedCity = useDebounce(city);

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const { data, isLoading, isError, error, isFetching } = useCustomerList({
    page,
    pageSize,
    searchTerm: debouncedSearch === "" ? undefined : debouncedSearch,
    city: debouncedCity === "" ? undefined : debouncedCity,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Müşteriler</h1>
        <p className="text-muted-foreground">Kayıtlı müşteriler, risk objeleri ve iletişim bilgileri.</p>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64 space-y-2">
          <Label htmlFor="customerSearch">Ara</Label>
          <Input
            id="customerSearch"
            placeholder="Ad, soyad, TCKN, e-posta veya telefon"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="w-48 space-y-2">
          <Label htmlFor="customerCity">İl</Label>
          <Input
            id="customerCity"
            placeholder="ör. İstanbul"
            value={city}
            onChange={(event) => {
              setCity(event.target.value);
              setPage(1);
            }}
          />
        </div>
      </div>

      {isLoading ? (
        <SkeletonRows rows={6} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<UsersIcon />}
            title="Müşteri bulunamadı"
            description="Filtrelerle eşleşen müşteri yok. Ad, soyad, e-posta veya telefon ile aramayı değiştirin."
          />
        </Card>
      ) : (
        <>
          <Card className={isFetching ? "opacity-60" : undefined}>
            <CardContent className="overflow-x-auto p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-muted-foreground">
                    <th className="px-4 py-3 font-medium">Ad Soyad</th>
                    <th className="px-4 py-3 font-medium">TCKN</th>
                    <th className="px-4 py-3 font-medium">Telefon</th>
                    <th className="px-4 py-3 font-medium">İl</th>
                    <th className="px-4 py-3 font-medium">Kayıt Tarihi</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((customer) => (
                    <tr
                      key={customer.id}
                      onClick={() => setSelectedId(customer.id)}
                      className="cursor-pointer border-b last:border-0 transition-colors hover:bg-accent/50"
                    >
                      <td className="px-4 py-3 font-medium">
                        {customer.firstName} {customer.lastName}
                      </td>
                      <td className="px-4 py-3 font-mono">{customer.maskedTckn}</td>
                      <td className="px-4 py-3">{customer.phoneNumber}</td>
                      <td className="px-4 py-3">{customer.city}</td>
                      <td className="px-4 py-3">{formatDate(customer.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
          <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} totalCount={data.totalCount}>
            <PageSizeSelector value={pageSize} onChange={handlePageSizeChange} />
          </Pagination>
        </>
      )}

      <Drawer
        open={selectedId !== null}
        onClose={() => setSelectedId(null)}
        title="Müşteri Detayı"
        description="Profil, iletişim ve risk objeleri"
      >
        {selectedId !== null && <CustomerDetailPanel customerId={selectedId} />}
      </Drawer>
    </div>
  );
}

import { useState } from "react";
import { CreateStaffForm } from "@/features/staff/components/CreateStaffForm";
import { StaffDetailPanel } from "@/features/staff/components/StaffDetailPanel";
import { StaffStatusBadge } from "@/features/staff/components/StaffStatusBadge";
import { useStaffList } from "@/features/staff/hooks/useStaff";
import {
  Alert,
  Button,
  Card,
  CardContent,
  Drawer,
  EmptyState,
  Input,
  Label,
  PageSizeSelector,
  Pagination,
  Select,
  SkeletonRows,
  UserIcon,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";

type StatusFilter = "all" | "active" | "passive";

function toIsActive(filter: StatusFilter): boolean | undefined {
  if (filter === "active") return true;
  if (filter === "passive") return false;
  return undefined;
}

/**
 * Personel Yönetimi (yalnızca Admin — route `/admin/staff` `ProtectedRoute[Admin]` ile korunur).
 * Arama (e-posta/ad) + aktiflik filtresi + tablo + oluşturma/detay çekmeceleri. Rol değiştirme,
 * Admin oluşturma, şifre sıfırlama ve silme UI'ı BİLİNÇLİ olarak yoktur.
 */
export default function AdminStaffListPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const debouncedSearch = useDebounce(searchTerm);

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const { data, isLoading, isError, error, isFetching } = useStaffList({
    page,
    pageSize,
    searchTerm: debouncedSearch === "" ? undefined : debouncedSearch,
    isActive: toIsActive(statusFilter),
  });

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Personel</h1>
          <p className="text-muted-foreground">Acente personel hesapları ve erişim durumu.</p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>Yeni Personel</Button>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-64 space-y-2">
          <Label htmlFor="staffSearch">Ara</Label>
          <Input
            id="staffSearch"
            placeholder="Ad soyad veya e-posta"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPage(1);
            }}
          />
        </div>
        <div className="w-48 space-y-2">
          <Label htmlFor="staffStatus">Durum</Label>
          <Select
            id="staffStatus"
            value={statusFilter}
            onChange={(event) => {
              setStatusFilter(event.target.value as StatusFilter);
              setPage(1);
            }}
          >
            <option value="all">Tümü</option>
            <option value="active">Aktif</option>
            <option value="passive">Pasif</option>
          </Select>
        </div>
      </div>

      {isLoading ? (
        <SkeletonRows rows={6} />
      ) : isError || data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(error)[0]}</Alert>
      ) : data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<UserIcon />}
            title="Personel bulunamadı"
            description="Filtrelerle eşleşen personel yok. Aramayı değiştirin veya yeni personel oluşturun."
            action={<Button onClick={() => setCreateOpen(true)}>Yeni Personel</Button>}
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
                    <th className="px-4 py-3 font-medium">E-posta</th>
                    <th className="px-4 py-3 font-medium">Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((staff) => (
                    <tr
                      key={staff.id}
                      onClick={() => setSelectedId(staff.id)}
                      className="cursor-pointer border-b last:border-0 transition-colors hover:bg-accent/50"
                    >
                      <td className="px-4 py-3 font-medium">{staff.fullName ?? "—"}</td>
                      <td className="px-4 py-3">{staff.email}</td>
                      <td className="px-4 py-3">
                        <StaffStatusBadge isActive={staff.isActive} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            onPageChange={setPage}
            totalCount={data.totalCount}
          >
            <PageSizeSelector value={pageSize} onChange={handlePageSizeChange} />
          </Pagination>
        </>
      )}

      <Drawer
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Yeni Personel"
        description="Acente için yeni bir personel hesabı oluşturun"
      >
        <CreateStaffForm onCreated={() => setCreateOpen(false)} />
      </Drawer>

      <Drawer
        open={selectedId !== null}
        onClose={() => setSelectedId(null)}
        title="Personel Detayı"
        description="Bilgi düzenleme ve hesap durumu yönetimi"
      >
        {selectedId !== null && <StaffDetailPanel staffId={selectedId} />}
      </Drawer>
    </div>
  );
}

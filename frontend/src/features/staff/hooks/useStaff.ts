import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  createStaff,
  getStaffById,
  getStaffList,
  setStaffStatus,
  updateStaff,
} from "@/features/staff/services/staffApi";
import type {
  CreateStaffRequest,
  SetStaffStatusRequest,
  StaffListParams,
  UpdateStaffRequest,
} from "@/features/staff/types/staff.types";

export const staffQueryKeys = {
  all: ["staff"] as const,
  list: (params: StaffListParams) => ["staff", "list", params] as const,
  detail: (id: string) => ["staff", "detail", id] as const,
};

/** Personel listesi (sayfalı; e-posta/ad araması + aktiflik filtresi). */
export function useStaffList(params: StaffListParams) {
  return useQuery({
    queryKey: staffQueryKeys.list(params),
    queryFn: () => getStaffList(params),
  });
}

/** Personel detayı; çekmece kapalıyken (`id === null`) sorgu çalışmaz. */
export function useStaffDetail(id: string | null) {
  return useQuery({
    queryKey: staffQueryKeys.detail(id ?? ""),
    queryFn: () => getStaffById(id ?? ""),
    enabled: id !== null,
  });
}

/** Yeni personel oluşturur. Başarıda liste tazelenir + başarı bildirimi gösterilir. */
export function useCreateStaff() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateStaffRequest) => createStaff(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: staffQueryKeys.all });
      toast.success("Personel hesabı oluşturuldu.");
    },
  });
}

/** Personelin görünen adını günceller. Başarıda liste + detay tazelenir. */
export function useUpdateStaff() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateStaffRequest }) =>
      updateStaff(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: staffQueryKeys.all });
      toast.success("Personel bilgileri güncellendi.");
    },
  });
}

/**
 * Personeli aktif/pasif yapar. Başarıda liste + detay tazelenir. Toplu token iptali backend'de
 * yapılır — frontend yalnızca durum değişimini tetikler ve sonucu bildirir.
 */
export function useSetStaffStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: SetStaffStatusRequest }) =>
      setStaffStatus(id, request),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: staffQueryKeys.all });
      toast.success(variables.request.isActive ? "Personel aktifleştirildi." : "Personel pasifleştirildi.");
    },
  });
}

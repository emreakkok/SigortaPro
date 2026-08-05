import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addProperty,
  addVehicle,
  changePassword,
  getMyProfile,
  updateProfile,
  updateVehicle,
} from "@/features/profile/services/profileApi";
import type {
  PropertyRequest,
  UpdateProfileRequest,
  VehicleRequest,
} from "@/features/profile/types/profile.types";

/** Profil sorgusu anahtarı — teklif sihirbazı da risk objelerini buradan okur. */
export const profileQueryKey = ["profile", "me"] as const;

export function useMyProfile(enabled: boolean = true) {
  return useQuery({
    queryKey: profileQueryKey,
    queryFn: getMyProfile,
    // ADR-039: UserMenu gibi rol-karışık yüzeyler, personel oturumunda sorguyu kapatabilir
    // (personelin /customers/me ucu yoktur → 403'e hiç gidilmez). Varsayılan davranış değişmez.
    enabled,
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateProfileRequest) => updateProfile(request),
    onSuccess: (profile) => queryClient.setQueryData(profileQueryKey, profile),
  });
}

/**
 * customerId verilirse acente destekli (personel, müşteri adına araç ekler) → o müşterinin detay sorgusu
 * tazelenir; verilmezse self-servis → oturum sahibinin profili tazelenir.
 */
export function useAddVehicle(customerId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: VehicleRequest) => addVehicle(request, customerId),
    // Araç DTO'su profilin bir alt kümesidir; ilgili kaynağı yeniden çekmek en basit tutarlılık yolu.
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: customerId ? ["customers"] : profileQueryKey,
      }),
  });
}

export function useUpdateVehicle(vehicleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: VehicleRequest) => updateVehicle(vehicleId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profileQueryKey }),
  });
}

/** Şifre değiştirme mutation'ı (ADR-040) — oturum/token akışını etkilemez (JWT mimarisi değişmez). */
export function useChangePassword() {
  return useMutation({
    mutationFn: (request: { currentPassword: string; newPassword: string }) =>
      changePassword(request),
  });
}

/** customerId verilirse acente destekli (personel, müşteri adına konut ekler). */
export function useAddProperty(customerId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: PropertyRequest) => addProperty(request, customerId),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: customerId ? ["customers"] : profileQueryKey,
      }),
  });
}

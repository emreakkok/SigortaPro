import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addProperty,
  addVehicle,
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

export function useMyProfile() {
  return useQuery({
    queryKey: profileQueryKey,
    queryFn: getMyProfile,
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateProfileRequest) => updateProfile(request),
    onSuccess: (profile) => queryClient.setQueryData(profileQueryKey, profile),
  });
}

export function useAddVehicle() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: VehicleRequest) => addVehicle(request),
    // Araç DTO'su profilin bir alt kümesidir; profili yeniden çekmek en basit tutarlılık yolu.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profileQueryKey }),
  });
}

export function useUpdateVehicle(vehicleId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: VehicleRequest) => updateVehicle(vehicleId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profileQueryKey }),
  });
}

export function useAddProperty() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: PropertyRequest) => addProperty(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: profileQueryKey }),
  });
}

import { Badge } from "@/shared/components";

/** Personel aktiflik durumunu rozet olarak gösterir (yeşil = aktif, nötr = pasif). */
export function StaffStatusBadge({ isActive }: { isActive: boolean }) {
  return (
    <Badge variant={isActive ? "success" : "secondary"}>{isActive ? "Aktif" : "Pasif"}</Badge>
  );
}

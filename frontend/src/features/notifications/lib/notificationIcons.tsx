import type { ReactNode } from "react";
import {
  AlertTriangleIcon,
  BellIcon,
  FileTextIcon,
  ShieldCheckIcon,
  ShieldIcon,
  UsersIcon,
} from "@/shared/components";

/*
 * Olay türü → ikon. Bildirim merkezinde/zilde olayın ne olduğu bir bakışta anlaşılır.
 * Mevcut el yazımı ikon seti kullanılır; yeni ikon kütüphanesi eklenmez.
 */
export function notificationIcon(type: string): ReactNode {
  switch (type) {
    case "customer-registered":
      return <UsersIcon />;
    case "quote-created":
      return <FileTextIcon />;
    case "policy-created":
      return <ShieldCheckIcon />;
    case "claim-created":
    case "claim-status-changed":
      return <AlertTriangleIcon />;
    case "password-reset-requested":
      return <ShieldIcon />;
    default:
      return <BellIcon />;
  }
}

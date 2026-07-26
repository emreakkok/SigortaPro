import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getMyNotifications,
  getUnreadCount,
  markAllNotificationsAsRead,
  markNotificationAsRead,
} from "@/features/notifications/services/notificationsApi";
import type { NotificationListParams } from "@/features/notifications/types/notification.types";

/** Bildirim sorgu anahtarları (ADR-042) — SignalR invalidation'ı da bu kökü kullanır. */
export const notificationQueryKeys = {
  all: ["notifications"] as const,
  list: (params: NotificationListParams) => ["notifications", "list", params] as const,
  unreadCount: ["notifications", "unread-count"] as const,
};

export function useNotificationList(params: NotificationListParams, enabled: boolean = true) {
  return useQuery({
    queryKey: notificationQueryKeys.list(params),
    queryFn: () => getMyNotifications(params),
    enabled,
  });
}

export function useUnreadNotificationCount(enabled: boolean) {
  return useQuery({
    queryKey: notificationQueryKeys.unreadCount,
    queryFn: getUnreadCount,
    enabled,
  });
}

export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => markNotificationAsRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationQueryKeys.all }),
  });
}

export function useMarkAllNotificationsAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: markAllNotificationsAsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationQueryKeys.all }),
  });
}

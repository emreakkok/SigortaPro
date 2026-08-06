import { api } from "@/shared/lib/axios";
import type { PagedResult } from "@/shared/types/api.types";
import type { StoredNotificationDto, NotificationListParams } from "@/features/notifications/types/notification.types";

/** `GET /notifications` — oturum sahibinin kalıcı bildirim geçmişi. */
export async function getMyNotifications(
  params: NotificationListParams,
): Promise<PagedResult<StoredNotificationDto>> {
  const response = await api.get<PagedResult<StoredNotificationDto>>("/notifications", { params });
  return response.data;
}

/** `GET /notifications/unread-count` — zil rozeti sayacı. */
export async function getUnreadCount(): Promise<number> {
  const response = await api.get<number>("/notifications/unread-count");
  return response.data;
}

/** `POST /notifications/{id}/read` — tek bildirimi okundu işaretler. */
export async function markNotificationAsRead(id: string): Promise<void> {
  await api.post(`/notifications/${id}/read`);
}

/** `POST /notifications/read-all` — tüm okunmamışları okundu işaretler. */
export async function markAllNotificationsAsRead(): Promise<void> {
  await api.post("/notifications/read-all");
}

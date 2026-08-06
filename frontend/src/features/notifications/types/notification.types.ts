/** SignalR "notification" olayının yükü (backend `SignalRRealTimeNotifier.ToPayload` ile birebir). */
export interface AppNotification {
  id: string;
  type: string;
  severity: "success" | "info" | "warning" | "error";
  title: string;
  message: string;
  /** Kısa operasyonel referans (ör. poliçe numarası) — toast'ta gösterilir. */
  referenceCode: string | null;
  occurredAt: string;
}

/** Kalıcı bildirim kaydı (backend `NotificationDto`; actor/referans). */
export interface StoredNotificationDto {
  id: string;
  type: string;
  severity: "success" | "info" | "warning" | "error";
  title: string;
  message: string;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
  /** İşlemi yapan kullanıcının olay anındaki görünen adı (snapshot). */
  actorName: string | null;
  /** Operasyonel referans (ör. poliçe numarası); karşılığı olmayan kayıtlarda null. */
  referenceCode: string | null;
}

/** `GET /notifications` sorgu parametreleri. */
export interface NotificationListParams {
  isRead?: boolean;
  severity?: string;
  searchTerm?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

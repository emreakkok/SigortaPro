import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  useMarkAllNotificationsAsRead,
  useMarkNotificationAsRead,
  useNotificationList,
  useUnreadNotificationCount,
} from "@/features/notifications/hooks/useNotificationQueries";
import { useNotifications } from "@/features/notifications/hooks/useNotifications";
import { notificationHref } from "@/features/notifications/lib/notificationNavigation";
import { SEVERITY_DOT } from "@/features/notifications/lib/severityStyles";
import { BellIcon, Spinner } from "@/shared/components";
import { cn } from "@/shared/lib/utils";

const timeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});

/**
 * Navbar bildirim zili v2 (ADR-042): rozet + son bildirimler artık sunucudan (kalıcı) okunur;
 * okundu işaretleme DB'ye yazılır. Bağlantı durumu zil üzerinde gösterilir; tam geçmiş için
 * "Tümünü gör" → Bildirim Merkezi. Layout'lara routes.tsx'ten slot olarak verilir (ADR-029).
 */
export function NotificationBell() {
  const { connectionState } = useNotifications();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const unread = useUnreadNotificationCount(true);
  const unreadCount = unread.data ?? 0;
  // Dropdown yalnızca açıkken son bildirimleri çeker (gereksiz istek yok).
  const recent = useNotificationList({ page: 1, pageSize: 7 }, open);
  const markAsRead = useMarkNotificationAsRead();
  const markAllAsRead = useMarkAllNotificationsAsRead();

  useEffect(() => {
    if (!open) {
      return;
    }
    function handleOutsideClick(event: MouseEvent) {
      if (containerRef.current !== null && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutsideClick);
    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("mousedown", handleOutsideClick);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [open]);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={
          unreadCount > 0 ? `Bildirimler (${unreadCount} okunmamış)` : "Bildirimler"
        }
        className="relative flex h-10 w-10 items-center justify-center rounded-full transition-colors hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <BellIcon className="h-5 w-5" />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-5 min-w-[1.25rem] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
        {connectionState !== "connected" && (
          <span
            aria-hidden="true"
            title="Canlı bildirim bağlantısı yok"
            className={cn(
              "absolute bottom-0 right-0 h-2.5 w-2.5 rounded-full border-2 border-card",
              connectionState === "reconnecting" || connectionState === "connecting"
                ? "bg-warning"
                : "bg-destructive",
            )}
          />
        )}
      </button>

      {open && (
        <div className="absolute right-0 z-30 mt-2 w-80 overflow-hidden rounded-lg border bg-card shadow-lg">
          <div className="flex items-center justify-between border-b px-3 py-2">
            <p className="text-sm font-semibold">Bildirimler</p>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => markAllAsRead.mutate()}
                disabled={markAllAsRead.isPending}
                className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
              >
                Tümünü okundu işaretle
              </button>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto">
            {recent.isLoading ? (
              <div className="flex justify-center py-8">
                <Spinner />
              </div>
            ) : recent.data === undefined || recent.data.items.length === 0 ? (
              <p className="px-3 py-8 text-center text-sm text-muted-foreground">
                Henüz bildirim yok. Yeni olaylar burada anlık görünecek.
              </p>
            ) : (
              <ul className="divide-y">
                {recent.data.items.map((notification) => (
                  <li key={notification.id}>
                    <button
                      type="button"
                      onClick={() => {
                        // ADR-047: tıklama → okundu işaretle + ilgili kayda git (hedef varsa).
                        if (!notification.isRead) {
                          markAsRead.mutate(notification.id);
                        }
                        const href = notificationHref(notification);
                        setOpen(false);
                        if (href !== null) {
                          navigate(href);
                        }
                      }}
                      className={cn(
                        "flex w-full items-start gap-2 px-3 py-2.5 text-left transition-colors hover:bg-accent/50",
                        !notification.isRead && "bg-accent/40",
                      )}
                    >
                      <span
                        aria-hidden="true"
                        className={cn(
                          "mt-1.5 h-2 w-2 shrink-0 rounded-full",
                          SEVERITY_DOT[notification.severity] ?? "bg-muted-foreground",
                          notification.isRead && "opacity-30",
                        )}
                      />
                      <span className="min-w-0 flex-1">
                        <span className="flex items-baseline justify-between gap-2">
                          <span className={cn("truncate text-sm", !notification.isRead && "font-semibold")}>
                            {notification.title}
                          </span>
                          <span className="shrink-0 text-[10px] text-muted-foreground">
                            {timeFormatter.format(new Date(notification.createdAt))}
                          </span>
                        </span>
                        <span className="block truncate text-xs text-muted-foreground">
                          {notification.message}
                        </span>
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="border-t px-3 py-2 text-center">
            <Link
              to="/admin/notifications"
              onClick={() => setOpen(false)}
              className="text-sm font-medium text-primary hover:underline"
            >
              Tüm bildirimleri gör
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}

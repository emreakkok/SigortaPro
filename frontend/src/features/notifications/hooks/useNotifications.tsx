import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useAuth } from "@/features/auth/hooks/useAuth";
import { notificationQueryKeys } from "@/features/notifications/hooks/useNotificationQueries";
import {
  DEFAULT_PREFERENCES,
  loadPreferences,
  preferenceGroupOf,
  savePreferences,
  type NotificationPreferences,
} from "@/features/notifications/lib/notificationPreferences";
import type { AppNotification } from "@/features/notifications/types/notification.types";
import { env } from "@/shared/lib/env";
import { getSession, hasAnyRole } from "@/shared/lib/session";
import { UserRoles } from "@/shared/types/auth.types";

export type ConnectionState = "disconnected" | "connecting" | "connected" | "reconnecting";

interface NotificationContextValue {
  /** SignalR bağlantı durumu (zil üzerindeki bağlantı göstergesi — ADR-042 dayanıklılık). */
  connectionState: ConnectionState;
  preferences: NotificationPreferences;
  updatePreferences: (next: NotificationPreferences) => void;
  /** Tarayıcı bildirimi iznini ister; sonucu tercihe yazar. */
  requestBrowserPermission: () => Promise<boolean>;
}

const NotificationContext = createContext<NotificationContextValue | null>(null);

/**
 * Gerçek zamanlı bildirim sağlayıcısı v2 (ADR-041/ADR-042). Staff oturumunda hub'a bağlanır; gelen
 * bildirimde (1) sunucu destekli bildirim sorgularını geçersizleştirir (zil/merkez/rozet DB'den tazelenir),
 * (2) tercihlere göre sonner toast'u gösterir, (3) sekme arka plandaysa ve izin verildiyse tarayıcı
 * bildirimi üretir, (4) olay türüne göre dashboard/liste cache'lerini geçersizleştirir. Kopma/yeniden
 * bağlanma kullanıcıya bildirilir; yeniden bağlanınca tam senkronizasyon yapılır (sayfa yenilemek gerekmez).
 */
export function NotificationProvider({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  const queryClient = useQueryClient();
  const [connectionState, setConnectionState] = useState<ConnectionState>("disconnected");
  const [preferences, setPreferences] = useState<NotificationPreferences>(DEFAULT_PREFERENCES);

  const isStaff =
    session !== null && hasAnyRole(session, [UserRoles.Admin, UserRoles.Personel]);
  const userId = session?.userId ?? null;

  // Tercihler kullanıcı başına saklanır; oturum değişince yeniden yüklenir.
  useEffect(() => {
    setPreferences(userId === null ? DEFAULT_PREFERENCES : loadPreferences(userId));
  }, [userId]);

  const updatePreferences = useCallback(
    (next: NotificationPreferences) => {
      setPreferences(next);
      if (userId !== null) {
        savePreferences(userId, next);
      }
    },
    [userId],
  );

  const requestBrowserPermission = useCallback(async (): Promise<boolean> => {
    if (!("Notification" in window)) {
      return false;
    }
    const permission =
      Notification.permission === "granted" ? "granted" : await Notification.requestPermission();
    return permission === "granted";
  }, []);

  /** Sunucu durumunu tam senkronize eder (ilk bağlantı + yeniden bağlanma). */
  const resync = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: notificationQueryKeys.all });
    void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  }, [queryClient]);

  useEffect(() => {
    if (!isStaff) {
      setConnectionState("disconnected");
      return;
    }

    const hubUrl = `${env.apiBaseUrl.replace(/\/api\/v1\/?$/, "")}/hubs/notifications`;
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getSession()?.accessToken ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const handleIncoming = (notification: AppNotification) => {
      // Kalıcı kayıt zaten sunucuda; zil/merkez/rozet DB'den tazelenir (tek doğruluk kaynağı).
      void queryClient.invalidateQueries({ queryKey: notificationQueryKeys.all });

      // Olay türü → etkilenen sunucu durumu (dashboard KPI/grafikler + ilgili listeler canlı güncellenir).
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      if (notification.type.startsWith("claim-")) {
        void queryClient.invalidateQueries({ queryKey: ["claims"] });
      }
      if (notification.type === "quote-created" || notification.type === "policy-created") {
        void queryClient.invalidateQueries({ queryKey: ["quotes"] });
      }
      if (notification.type === "customer-registered") {
        void queryClient.invalidateQueries({ queryKey: ["customers"] });
      }

      // Uygulama-içi teslim kanalları tercihe tabidir (geçmiş her koşulda sunucuda saklanır).
      const prefs = getSessionPreferences();
      if (!prefs[preferenceGroupOf(notification.type)]) {
        return;
      }

      const show =
        notification.severity === "success"
          ? toast.success
          : notification.severity === "warning"
            ? toast.warning
            : notification.severity === "error"
              ? toast.error
              : toast.info;
      // ADR-047: toast KISA ve anlıktır (olay + varsa referans); detaylı anlatım (kim/kimin için/tutar)
      // Bildirim Merkezi'ne aittir. Uzun mesaj toast'a taşınmaz.
      show(notification.title, {
        description: notification.referenceCode ?? undefined,
      });

      // Sekme arka plandaysa işletim sistemi bildirimi (izin + tercih varsa).
      if (
        prefs.browserNotifications &&
        document.hidden &&
        "Notification" in window &&
        Notification.permission === "granted"
      ) {
        new Notification(notification.title, { body: notification.message, tag: notification.id });
      }
    };

    // Tercihler event handler kapanışında bayatlamasın diye okuma anında yüklenir.
    const getSessionPreferences = (): NotificationPreferences =>
      userId === null ? DEFAULT_PREFERENCES : loadPreferences(userId);

    connection.on("notification", handleIncoming);
    connection.onreconnecting(() => {
      setConnectionState("reconnecting");
      toast.warning("Canlı bildirim bağlantısı koptu", {
        description: "Yeniden bağlanılıyor; kaçırılan bildirimler bağlantı kurulunca yüklenecek.",
        id: "signalr-connection",
      });
    });
    connection.onreconnected(() => {
      setConnectionState("connected");
      toast.success("Canlı bildirim bağlantısı yeniden kuruldu", { id: "signalr-connection" });
      // Kopukluk sırasındaki bildirimler DB'de — tam senkronizasyon sayfa yenilemeden yapılır.
      resync();
    });
    connection.onclose(() => setConnectionState("disconnected"));

    setConnectionState("connecting");
    connection
      .start()
      .then(() => {
        setConnectionState("connected");
        resync();
      })
      .catch(() => setConnectionState("disconnected"));

    return () => {
      connection.off("notification", handleIncoming);
      void connection.stop();
    };
  }, [isStaff, userId, queryClient, resync]);

  const value = useMemo<NotificationContextValue>(
    () => ({ connectionState, preferences, updatePreferences, requestBrowserPermission }),
    [connectionState, preferences, updatePreferences, requestBrowserPermission],
  );

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
}

export function useNotifications(): NotificationContextValue {
  const context = useContext(NotificationContext);
  if (context === null) {
    throw new Error("useNotifications, NotificationProvider içinde kullanılmalıdır.");
  }
  return context;
}

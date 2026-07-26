/**
 * Bildirim tercihleri (ADR-042). MVP kapsamı: yalnızca uygulama-içi teslim kanallarını (toast +
 * tarayıcı bildirimi) etkiler ve tarayıcıda (localStorage, kullanıcı başına anahtar) saklanır.
 * Kalıcı bildirim geçmişi TERCİHTEN BAĞIMSIZ her zaman sunucuya yazılır (denetim izi eksiksiz kalır).
 * Yapı, ileride sunucu-taraflı tercihe ve e-posta/mobil push kanallarına taşınmaya uygundur.
 */
export interface NotificationPreferences {
  /** Olay grubu bazlı uygulama-içi bildirim anahtarları. */
  policyEvents: boolean;
  claimEvents: boolean;
  quoteEvents: boolean;
  systemEvents: boolean;
  /** Sekme arka plandayken işletim sistemi bildirimi (Browser Notification API). */
  browserNotifications: boolean;
}

export const DEFAULT_PREFERENCES: NotificationPreferences = {
  policyEvents: true,
  claimEvents: true,
  quoteEvents: true,
  systemEvents: true,
  browserNotifications: false,
};

/** Olay türü → tercih grubu eşlemesi (behavior kataloğundaki Type değerleri). */
export function preferenceGroupOf(type: string): keyof Omit<NotificationPreferences, "browserNotifications"> {
  if (type === "policy-created") {
    return "policyEvents";
  }
  if (type === "claim-created" || type === "claim-status-changed") {
    return "claimEvents";
  }
  if (type === "quote-created") {
    return "quoteEvents";
  }
  return "systemEvents";
}

const STORAGE_PREFIX = "sigortapro.notification-preferences.";

export function loadPreferences(userId: string): NotificationPreferences {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + userId);
    if (raw === null) {
      return DEFAULT_PREFERENCES;
    }
    return { ...DEFAULT_PREFERENCES, ...(JSON.parse(raw) as Partial<NotificationPreferences>) };
  } catch {
    return DEFAULT_PREFERENCES;
  }
}

export function savePreferences(userId: string, preferences: NotificationPreferences): void {
  localStorage.setItem(STORAGE_PREFIX + userId, JSON.stringify(preferences));
}

import { useNotifications } from "@/features/notifications/hooks/useNotifications";
import type { NotificationPreferences } from "@/features/notifications/lib/notificationPreferences";

const GROUP_LABELS: { key: keyof Omit<NotificationPreferences, "browserNotifications">; label: string; description: string }[] = [
  { key: "policyEvents", label: "Yeni Poliçe", description: "Teklif satın alma ve poliçe düzenleme olayları." },
  { key: "claimEvents", label: "Yeni Hasar", description: "Hasar bildirimi ve hasar durum değişiklikleri." },
  { key: "quoteEvents", label: "Yeni Teklif", description: "Müşterilerin oluşturduğu yeni teklifler." },
  { key: "systemEvents", label: "Sistem Bildirimleri", description: "Yeni kayıt, şifre sıfırlama gibi sistem olayları." },
];

/**
 * Bildirim tercihleri (ADR-042). MVP: yalnızca uygulama-içi teslimi (toast + tarayıcı bildirimi)
 * etkiler; bildirim geçmişi her koşulda kaydedilir. Yapı ileride e-posta/mobil push kanallarına hazırdır.
 */
export function NotificationPreferencesPanel() {
  const { preferences, updatePreferences, requestBrowserPermission } = useNotifications();

  const toggleGroup = (key: keyof Omit<NotificationPreferences, "browserNotifications">) =>
    updatePreferences({ ...preferences, [key]: !preferences[key] });

  const toggleBrowser = async () => {
    if (!preferences.browserNotifications) {
      const granted = await requestBrowserPermission();
      updatePreferences({ ...preferences, browserNotifications: granted });
      return;
    }
    updatePreferences({ ...preferences, browserNotifications: false });
  };

  return (
    <div className="space-y-4">
      <ul className="divide-y rounded-lg border">
        {GROUP_LABELS.map((group) => (
          <li key={group.key} className="flex items-center justify-between gap-4 px-4 py-3">
            <div>
              <p className="text-sm font-medium">{group.label}</p>
              <p className="text-xs text-muted-foreground">{group.description}</p>
            </div>
            <label className="inline-flex cursor-pointer items-center">
              <input
                type="checkbox"
                className="accent-primary h-4 w-4"
                checked={preferences[group.key]}
                onChange={() => toggleGroup(group.key)}
                aria-label={`${group.label} bildirimleri`}
              />
            </label>
          </li>
        ))}
        <li className="flex items-center justify-between gap-4 px-4 py-3">
          <div>
            <p className="text-sm font-medium">Tarayıcı Bildirimleri</p>
            <p className="text-xs text-muted-foreground">
              Sekme arka plandayken kritik olaylar işletim sistemi bildirimi olarak gösterilir
              (tarayıcı izni gerektirir).
            </p>
          </div>
          <label className="inline-flex cursor-pointer items-center">
            <input
              type="checkbox"
              className="accent-primary h-4 w-4"
              checked={preferences.browserNotifications}
              onChange={() => void toggleBrowser()}
              aria-label="Tarayıcı bildirimleri"
            />
          </label>
        </li>
      </ul>
      <p className="text-xs text-muted-foreground">
        Tercihler yalnızca anlık uyarıları (toast/tarayıcı) etkiler; bildirim geçmişiniz her koşulda
        Bildirim Merkezi'nde saklanır.
      </p>
    </div>
  );
}

import type { ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "sonner";
import { AuthProvider } from "@/features/auth/hooks/useAuth";
import { NotificationProvider } from "@/features/notifications/hooks/useNotifications";
import { queryClient } from "@/shared/lib/queryClient";
import { ThemeProvider, useTheme } from "@/shared/theme/useTheme";

interface AppProvidersProps {
  children: ReactNode;
}

/**
 * Uygulama genel provider kompozisyonu. main.tsx ve App.tsx değişmez.
 * ThemeProvider en dışta durur → açık/koyu/sistem tema tüm ağaca uygulanır.
 * NotificationProvider: staff oturumunda SignalR bağlantısını yönetir; sonner Toaster'ı
 * gerçek zamanlı bildirim toast'larını gösterir (richColors → success/info/warning/error varyantları)
 * ve etkin temayı izler → toast'lar koyu modda da uyumlu.
 */
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <NotificationProvider>
            {children}
            <ThemedToaster />
          </NotificationProvider>
        </AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

/** sonner Toaster'ı seçili tema moduna bağlar (system → sonner işletim sistemini izler). */
function ThemedToaster() {
  const { mode } = useTheme();
  return <Toaster richColors closeButton position="top-right" theme={mode} />;
}

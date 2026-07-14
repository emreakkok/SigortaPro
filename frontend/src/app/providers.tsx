import type { ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider } from "@/features/auth/hooks/useAuth";
import { queryClient } from "@/shared/lib/queryClient";

interface AppProvidersProps {
  children: ReactNode;
}

/**
 * Uygulama genel provider kompozisyonu. Yeni provider'lar (ileride toast/tema)
 * buraya eklenir — main.tsx ve App.tsx değişmez.
 */
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  );
}

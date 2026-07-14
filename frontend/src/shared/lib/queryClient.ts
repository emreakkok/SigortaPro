import { QueryClient } from "@tanstack/react-query";

/**
 * Uygulama genelinde tek QueryClient (server state — TanStack Query).
 * staleTime > 0: aynı veriye kısa aralıklarla tekrar istek atılmaz
 * (DEVELOPMENT_RULES.md §6 — uygun staleTime ile cache).
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

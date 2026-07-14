import path from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    // Backend CORS allowlist'i (Cors:AllowedOrigins) bu origin'i bekler; port sabitlenir.
    port: 5173,
    strictPort: true,
  },
});

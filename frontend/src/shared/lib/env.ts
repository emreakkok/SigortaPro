/**
 * Ortam değişkenlerine tek erişim noktası: bileşenler/servisler `import.meta.env`'i
 * doğrudan okumaz, tip güvenli bu modülü kullanır.
 */
export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5153/api/v1",
} as const;

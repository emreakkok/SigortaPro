/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Backend API kök adresi (versiyon dahil), ör. http://localhost:5153/api/v1 */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

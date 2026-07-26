/*
 * Sayfalama varsayılanları (ADR-045). Tek merkez → Portal ve Admin farkı burada yönetilir.
 * Backend `pageSize` parametresini zaten kabul eder; bu dosya yalnızca istemci varsayılan/seçimidir
 * (yeni API yok, backend'e ek yük yok).
 */

/** Müşteri portalı: kullanıcı az kayıt yönetir → kart tabanlı listelerde rahat gezinme için düşük sabit. */
export const PORTAL_PAGE_SIZE = 6;

/** Admin: profesyonel veri yönetimi → kullanıcı seçebilir; varsayılan 20. */
export const ADMIN_PAGE_SIZE_OPTIONS = [10, 20, 50, 100] as const;
export const DEFAULT_ADMIN_PAGE_SIZE = 20;

export type AdminPageSize = (typeof ADMIN_PAGE_SIZE_OPTIONS)[number];

const STORAGE_KEY = "sigortapro.admin-page-size";

function isAdminPageSize(value: number): value is AdminPageSize {
  return (ADMIN_PAGE_SIZE_OPTIONS as readonly number[]).includes(value);
}

/** localStorage'dan admin sayfa boyutu tercihini okur; geçersiz/yoksa varsayılana düşer. */
export function readAdminPageSize(): AdminPageSize {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw !== null) {
      const parsed = Number(raw);
      if (isAdminPageSize(parsed)) {
        return parsed;
      }
    }
  } catch {
    /* localStorage erişilemezse varsayılan kullanılır */
  }
  return DEFAULT_ADMIN_PAGE_SIZE;
}

/** Admin sayfa boyutu tercihini kalıcılaştırır (tüm admin tabloları için paylaşılır). */
export function storeAdminPageSize(size: AdminPageSize): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(size));
  } catch {
    /* kalıcılık yoksa oturum içi çalışmaya devam edilir */
  }
}

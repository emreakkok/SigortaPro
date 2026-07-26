import { useCallback, useState } from "react";
import {
  readAdminPageSize,
  storeAdminPageSize,
  type AdminPageSize,
} from "@/shared/lib/pagination";

/*
 * Admin tablolarının sayfa boyutu tercihi (ADR-045). localStorage'da kalıcıdır ve tüm admin
 * tabloları arasında paylaşılır ("50 satır isterim" her yerde geçerli). Backend'e yük getirmez.
 */
export function useAdminPageSize(): [AdminPageSize, (size: AdminPageSize) => void] {
  const [pageSize, setPageSizeState] = useState<AdminPageSize>(() => readAdminPageSize());

  const setPageSize = useCallback((size: AdminPageSize) => {
    setPageSizeState(size);
    storeAdminPageSize(size);
  }, []);

  return [pageSize, setPageSize];
}

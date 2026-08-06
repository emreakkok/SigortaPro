import { useEffect, useState } from "react";

/**
 * Değeri verilen gecikmeyle geciktirir — arama input'ları her tuş vuruşunda API'ye
 * gitmesin diye.
 */
export function useDebounce<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

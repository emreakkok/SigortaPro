import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";

/*
 * Liste ekranlarındaki detay çekmecesinin seçili kaydı. `?focus=<id>` query parametresiyle
 * derin bağlantı kurulabilir → bildirimden tıklanınca ilgili kayıt doğrudan açılır. API'si
 * `useState<string | null>` ile aynıdır; mevcut sayfalar tek satır değişiklikle bunu kullanabilir.
 * Çekmece kapatıldığında parametre URL'den temizlenir (geri tuşu geçmişi kirlenmesin diye `replace`).
 */
export function useFocusedRecord(): [string | null, (id: string | null) => void] {
  const [searchParams, setSearchParams] = useSearchParams();
  const focusId = searchParams.get("focus");
  const [selectedId, setSelectedId] = useState<string | null>(focusId);

  // Kullanıcı zaten sayfadayken başka bir bildirime tıklarsa (aynı route, farklı focus) senkron kalınır.
  useEffect(() => {
    if (focusId !== null) {
      setSelectedId(focusId);
    }
  }, [focusId]);

  const select = useCallback(
    (id: string | null) => {
      setSelectedId(id);
      if (id === null && searchParams.has("focus")) {
        const next = new URLSearchParams(searchParams);
        next.delete("focus");
        setSearchParams(next, { replace: true });
      }
    },
    [searchParams, setSearchParams],
  );

  return [selectedId, select];
}

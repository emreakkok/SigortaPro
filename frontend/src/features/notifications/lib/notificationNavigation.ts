import type { StoredNotificationDto } from "@/features/notifications/types/notification.types";

/*
 * Bildirim → ilgili kayıt navigasyonu (ADR-047). Admin detayları ayrı route değil, liste ekranındaki
 * Drawer'dır; bu yüzden derin bağlantı `?focus=<id>` query parametresiyle kurulur (yeni route açılmaz).
 * İlgili liste sayfası bu parametreyi `useFocusedRecord` ile okuyup çekmeceyi açar; detay paneli veriyi
 * id ile kendisi çektiğinden kayıt o an listede/sayfada olmasa bile açılır.
 * RelatedEntityId taşımayan sistem bildirimlerinde (ör. şifre sıfırlama) navigasyon YAPILMAZ → null.
 */
const LIST_ROUTES: Record<string, string> = {
  Quote: "/admin/quotes",
  Policy: "/admin/policies",
  Claim: "/admin/claims",
  Customer: "/admin/customers",
};

type NavigableNotification = Pick<StoredNotificationDto, "relatedEntityId" | "relatedEntityType">;

/** Bildirimin hedef adresi; hedef yoksa null (bileşen bu durumda bağlantı göstermez). */
export function notificationHref(notification: NavigableNotification): string | null {
  const { relatedEntityType, relatedEntityId } = notification;
  if (relatedEntityType === null) {
    return null;
  }

  const route = LIST_ROUTES[relatedEntityType];
  if (route === undefined) {
    return null;
  }

  // Kayıt kimliği yoksa (ör. yeni müşteri kaydı) yalnızca ilgili listeye götürülür.
  return relatedEntityId === null ? route : `${route}?focus=${relatedEntityId}`;
}

/** Navigasyon bağlantısının etiketi — hedef türüne göre operasyonel eylem metni. */
export function notificationLinkLabel(relatedEntityType: string | null): string {
  switch (relatedEntityType) {
    case "Quote":
      return "Teklifi görüntüle";
    case "Policy":
      return "Poliçeyi görüntüle";
    case "Claim":
      return "Hasar dosyasını görüntüle";
    case "Customer":
      return "Müşterileri görüntüle";
    default:
      return "Görüntüle";
  }
}

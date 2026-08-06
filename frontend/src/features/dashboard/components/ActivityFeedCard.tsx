import { Link } from "react-router-dom";
import { useNotificationList } from "@/features/notifications/hooks/useNotificationQueries";
import { notificationHref } from "@/features/notifications/lib/notificationNavigation";
import {
  BellIcon,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  Skeleton,
} from "@/shared/components";

const FEED_SIZE = 8;

const relativeFormatter = new Intl.RelativeTimeFormat("tr-TR", { numeric: "auto" });

/** "2 dakika önce" gibi göreli zaman — akışın operasyonel tazeliğini gösterir. */
function relativeTime(iso: string): string {
  const diffMs = new Date(iso).getTime() - Date.now();
  const diffMinutes = Math.round(diffMs / 60000);

  if (Math.abs(diffMinutes) < 60) {
    return relativeFormatter.format(diffMinutes, "minute");
  }
  const diffHours = Math.round(diffMinutes / 60);
  if (Math.abs(diffHours) < 24) {
    return relativeFormatter.format(diffHours, "hour");
  }
  return relativeFormatter.format(Math.round(diffHours / 24), "day");
}

/**
 * Son Aktiviteler — MEVCUT bildirim altyapısından beslenir: actor snapshot'ı,
 * referans kodu ve `RelatedEntityId` zaten kayıtlıdır. **Yeni bir audit log sistemi kurulmaz**;
 * bildirim ≠ audit ayrımı korunur ve yeni endpoint eklenmez (mevcut `GET /notifications` küçük sayfa boyutuyla).
 * Satırlar, bildirim navigasyonunun aynısıyla (`?focus=<id>`) ilgili kayda götürür.
 */
export function ActivityFeedCard() {
  const { data, isLoading, isError } = useNotificationList({ page: 1, pageSize: FEED_SIZE });

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <CardTitle>Son Aktiviteler</CardTitle>
            <CardDescription>Acentede en son ne oldu?</CardDescription>
          </div>
          <Link
            to="/admin/notifications"
            className="text-sm font-medium text-primary underline-offset-4 hover:underline"
          >
            Tümünü gör →
          </Link>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="flex gap-3">
                <Skeleton className="h-2 w-2 shrink-0 rounded-full" />
                <div className="flex-1 space-y-1.5">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/4" />
                </div>
              </div>
            ))}
          </div>
        ) : isError || data === undefined ? (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Aktivite akışı şu anda yüklenemedi.
          </p>
        ) : data.items.length === 0 ? (
          <EmptyState
            icon={<BellIcon />}
            title="Henüz aktivite yok"
            description="Teklif, poliçe ve hasar hareketleri burada canlı olarak görünür."
          />
        ) : (
          <ul className="space-y-3">
            {data.items.map((item) => {
              const href = notificationHref(item);
              const body = (
                <div className="flex gap-3">
                  <span
                    aria-hidden="true"
                    className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-primary"
                  />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm">
                      <span className="font-medium">{item.title}</span>
                      {item.actorName !== null && (
                        <span className="text-muted-foreground"> · {item.actorName}</span>
                      )}
                    </p>
                    <p className="truncate text-xs text-muted-foreground">{item.message}</p>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      {relativeTime(item.createdAt)}
                      {item.referenceCode !== null && (
                        <span className="ml-2 font-mono">{item.referenceCode}</span>
                      )}
                    </p>
                  </div>
                </div>
              );

              return (
                <li key={item.id}>
                  {href === null ? (
                    body
                  ) : (
                    <Link to={href} className="block rounded-md transition-colors hover:bg-accent/50">
                      {body}
                    </Link>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

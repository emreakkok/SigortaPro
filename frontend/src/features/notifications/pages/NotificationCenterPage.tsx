import { useState } from "react";
import {
  useMarkAllNotificationsAsRead,
  useMarkNotificationAsRead,
  useNotificationList,
} from "@/features/notifications/hooks/useNotificationQueries";
import { Link } from "react-router-dom";
import { NotificationPreferencesPanel } from "@/features/notifications/components/NotificationPreferencesPanel";
import { notificationIcon } from "@/features/notifications/lib/notificationIcons";
import {
  notificationHref,
  notificationLinkLabel,
} from "@/features/notifications/lib/notificationNavigation";
import {
  SEVERITY_BADGE,
  SEVERITY_DOT,
  SEVERITY_LABELS,
} from "@/features/notifications/lib/severityStyles";
import {
  Alert,
  BellIcon,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  EmptyState,
  Input,
  Label,
  PageSizeSelector,
  Pagination,
  Select,
  SkeletonRows,
  Spinner,
} from "@/shared/components";
import { useAdminPageSize } from "@/shared/hooks/useAdminPageSize";
import { useDebounce } from "@/shared/hooks/useDebounce";
import { getApiErrorMessages } from "@/shared/lib/apiError";
import type { AdminPageSize } from "@/shared/lib/pagination";
import { cn } from "@/shared/lib/utils";

const dateTimeFormatter = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

/**
 * Bildirim Merkezi: kalıcı bildirim geçmişi — okunma/önem filtreleri, metin araması,
 * tarih aralığı ve sayfalama. Zil son bildirimleri gösterir; tam geçmiş buradadır.
 */
export default function NotificationCenterPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useAdminPageSize();
  const [readFilter, setReadFilter] = useState<"" | "read" | "unread">("");
  const [severity, setSeverity] = useState("");
  const [searchTerm, setSearchTerm] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");

  const debouncedSearch = useDebounce(searchTerm);

  const handlePageSizeChange = (size: AdminPageSize) => {
    setPageSize(size);
    setPage(1);
  };

  const list = useNotificationList({
    page,
    pageSize,
    isRead: readFilter === "" ? undefined : readFilter === "read",
    severity: severity === "" ? undefined : severity,
    searchTerm: debouncedSearch === "" ? undefined : debouncedSearch,
    from: from === "" ? undefined : from,
    to: to === "" ? undefined : `${to}T23:59:59`,
  });
  const markAsRead = useMarkNotificationAsRead();
  const markAllAsRead = useMarkAllNotificationsAsRead();

  const resetPage = () => setPage(1);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Bildirim Merkezi</h1>
          <p className="text-muted-foreground">
            Tüm sistem olaylarının kalıcı geçmişi; filtreleyin, arayın, okundu işaretleyin.
          </p>
        </div>
        <Button variant="outline" onClick={() => markAllAsRead.mutate()} disabled={markAllAsRead.isPending}>
          {markAllAsRead.isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Tümünü okundu işaretle"}
        </Button>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="w-56 space-y-2">
          <Label htmlFor="notificationSearch">Ara</Label>
          <Input
            id="notificationSearch"
            placeholder="Başlık veya mesaj"
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              resetPage();
            }}
          />
        </div>
        <div className="w-40 space-y-2">
          <Label htmlFor="readFilter">Durum</Label>
          <Select
            id="readFilter"
            value={readFilter}
            onChange={(event) => {
              setReadFilter(event.target.value as "" | "read" | "unread");
              resetPage();
            }}
          >
            <option value="">Tümü</option>
            <option value="unread">Okunmamış</option>
            <option value="read">Okunmuş</option>
          </Select>
        </div>
        <div className="w-40 space-y-2">
          <Label htmlFor="severityFilter">Tür</Label>
          <Select
            id="severityFilter"
            value={severity}
            onChange={(event) => {
              setSeverity(event.target.value);
              resetPage();
            }}
          >
            <option value="">Tümü</option>
            <option value="success">Başarılı</option>
            <option value="info">Bilgi</option>
            <option value="warning">Uyarı</option>
            <option value="error">Hata</option>
          </Select>
        </div>
        <div className="w-40 space-y-2">
          <Label htmlFor="fromDate">Başlangıç</Label>
          <Input
            id="fromDate"
            type="date"
            value={from}
            onChange={(event) => {
              setFrom(event.target.value);
              resetPage();
            }}
          />
        </div>
        <div className="w-40 space-y-2">
          <Label htmlFor="toDate">Bitiş</Label>
          <Input
            id="toDate"
            type="date"
            value={to}
            onChange={(event) => {
              setTo(event.target.value);
              resetPage();
            }}
          />
        </div>
      </div>

      {list.isLoading ? (
        <SkeletonRows rows={5} />
      ) : list.isError || list.data === undefined ? (
        <Alert variant="destructive">{getApiErrorMessages(list.error)[0]}</Alert>
      ) : list.data.items.length === 0 ? (
        <Card>
          <EmptyState
            icon={<BellIcon />}
            title="Bildirim yok"
            description="Filtrelere uyan bir bildirim bulunamadı. Yeni müşteri, teklif, poliçe ve hasar olayları burada listelenir."
          />
        </Card>
      ) : (
        <>
          {/*
            operasyonel activity-feed. Her kayıt "ne oldu / kim yaptı / hangi kayıt / ne zaman"
            sorularını tek başına cevaplar ve ilgili kayda tek tıkla götürür.
          */}
          <ul className="divide-y rounded-lg border bg-card">
            {list.data.items.map((notification) => {
              const href = notificationHref(notification);
              return (
                <li
                  key={notification.id}
                  className={cn("flex items-start gap-3 px-4 py-4", !notification.isRead && "bg-accent/30")}
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-accent text-accent-foreground [&_svg]:h-[1.1rem] [&_svg]:w-[1.1rem]",
                      !notification.isRead && "ring-2 ring-primary/30",
                    )}
                  >
                    {notificationIcon(notification.type)}
                  </span>

                  <div className="min-w-0 flex-1 space-y-1.5">
                    <div className="flex flex-wrap items-center gap-2">
                      {!notification.isRead && (
                        <span
                          aria-label="Okunmamış"
                          className={cn(
                            "h-2 w-2 shrink-0 rounded-full",
                            SEVERITY_DOT[notification.severity] ?? "bg-muted-foreground",
                          )}
                        />
                      )}
                      <p className={cn("text-sm", !notification.isRead && "font-semibold")}>
                        {notification.title}
                      </p>
                      <span
                        className={cn(
                          "rounded-full px-2 py-0.5 text-[10px] font-medium",
                          SEVERITY_BADGE[notification.severity] ?? "bg-muted text-muted-foreground",
                        )}
                      >
                        {SEVERITY_LABELS[notification.severity] ?? notification.severity}
                      </span>
                    </div>

                    <p className="text-sm text-muted-foreground">{notification.message}</p>

                    {/* Operasyonel künye: işlemi yapan + referans (varsa). */}
                    {(notification.actorName !== null || notification.referenceCode !== null) && (
                      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
                        {notification.actorName !== null && (
                          <span>
                            İşlemi yapan:{" "}
                            <span className="font-medium text-foreground">{notification.actorName}</span>
                          </span>
                        )}
                        {notification.referenceCode !== null && (
                          <span className="font-mono text-primary">{notification.referenceCode}</span>
                        )}
                      </div>
                    )}

                    <p className="text-xs text-muted-foreground">
                      {dateTimeFormatter.format(new Date(notification.createdAt))}
                      {notification.isRead && notification.readAt !== null && (
                        <> · Okundu: {dateTimeFormatter.format(new Date(notification.readAt))}</>
                      )}
                    </p>

                    {href !== null && (
                      <Link
                        to={href}
                        onClick={() => {
                          if (!notification.isRead) {
                            markAsRead.mutate(notification.id);
                          }
                        }}
                        className="inline-flex items-center gap-1 text-xs font-medium text-primary hover:underline"
                      >
                        {notificationLinkLabel(notification.relatedEntityType)} →
                      </Link>
                    )}
                  </div>

                  {!notification.isRead && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => markAsRead.mutate(notification.id)}
                      disabled={markAsRead.isPending}
                    >
                      Okundu işaretle
                    </Button>
                  )}
                </li>
              );
            })}
          </ul>

          <Pagination
            page={list.data.page}
            totalPages={list.data.totalPages}
            onPageChange={setPage}
            totalCount={list.data.totalCount}
          >
            <PageSizeSelector value={pageSize} onChange={handlePageSizeChange} />
          </Pagination>
        </>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Bildirim Tercihleri</CardTitle>
        </CardHeader>
        <CardContent>
          <NotificationPreferencesPanel />
        </CardContent>
      </Card>
    </div>
  );
}

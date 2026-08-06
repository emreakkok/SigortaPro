/** Önem düzeyi → kurumsal tema renkleri (toast/zil/merkez aynı sistemi kullanır). */
export const SEVERITY_DOT: Record<string, string> = {
  success: "bg-success",
  info: "bg-primary",
  warning: "bg-warning",
  error: "bg-destructive",
};

export const SEVERITY_BADGE: Record<string, string> = {
  success: "bg-success/15 text-success",
  info: "bg-primary/15 text-primary",
  warning: "bg-warning/15 text-warning",
  error: "bg-destructive/15 text-destructive",
};

export const SEVERITY_LABELS: Record<string, string> = {
  success: "Başarılı",
  info: "Bilgi",
  warning: "Uyarı",
  error: "Hata",
};

import { Link } from "react-router-dom";
import { Button } from "@/shared/components";

/** 403 — rol bu alana yetkili değil (ProtectedRoute buraya yönlendirir). */
export default function ForbiddenPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-4 text-center">
      <p className="text-7xl font-bold text-primary">403</p>
      <h1 className="text-2xl font-semibold">Erişim yetkiniz yok</h1>
      <p className="text-muted-foreground">
        Bu sayfa hesabınızın rolüne kapalı. Kendi alanınızdan devam edebilirsiniz.
      </p>
      <Link to="/">
        <Button variant="outline">Ana sayfaya dön</Button>
      </Link>
    </div>
  );
}

import { Link } from "react-router-dom";
import { Button } from "@/shared/components";

/** 401 — oturum geçersiz/süresi dolmuş (axios interceptor'ı buraya yönlendirir). */
export default function UnauthorizedPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-4 text-center">
      <p className="text-7xl font-bold text-primary">401</p>
      <h1 className="text-2xl font-semibold">Oturumunuz sona erdi</h1>
      <p className="text-muted-foreground">
        Güvenliğiniz için oturumunuz kapatıldı. Devam etmek için tekrar giriş yapın.
      </p>
      <Link to="/login">
        <Button>Giriş Yap</Button>
      </Link>
    </div>
  );
}

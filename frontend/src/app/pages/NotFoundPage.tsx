import { Link } from "react-router-dom";
import { Button } from "@/shared/components";

export default function NotFoundPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 p-4 text-center">
      <p className="text-7xl font-bold text-primary">404</p>
      <h1 className="text-2xl font-semibold">Sayfa bulunamadı</h1>
      <p className="text-muted-foreground">Aradığınız sayfa taşınmış veya hiç var olmamış olabilir.</p>
      <Link to="/">
        <Button variant="outline">Ana sayfaya dön</Button>
      </Link>
    </div>
  );
}

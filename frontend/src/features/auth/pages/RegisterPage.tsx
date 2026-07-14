import { Link } from "react-router-dom";
import { RegisterForm } from "@/features/auth/components/RegisterForm";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/components";

export default function RegisterPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4 py-8">
      <Card className="w-full max-w-2xl">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl text-primary">SigortaPro</CardTitle>
          <CardDescription>
            Müşteri hesabı oluşturun — kayıt sonrası otomatik giriş yapılır.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <RegisterForm />
        </CardContent>
        <CardFooter className="justify-center text-sm text-muted-foreground">
          <span>
            Zaten hesabınız var mı?{" "}
            <Link to="/login" className="font-medium text-primary hover:underline">
              Giriş yapın
            </Link>
          </span>
        </CardFooter>
      </Card>
    </div>
  );
}

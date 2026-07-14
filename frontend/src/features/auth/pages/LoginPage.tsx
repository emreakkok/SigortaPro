import { Link } from "react-router-dom";
import { LoginForm } from "@/features/auth/components/LoginForm";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/shared/components";

export default function LoginPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl text-primary">SigortaPro</CardTitle>
          <CardDescription>Hesabınıza giriş yapın.</CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm />
        </CardContent>
        <CardFooter className="justify-center text-sm text-muted-foreground">
          <span>
            Hesabınız yok mu?{" "}
            <Link to="/register" className="font-medium text-primary hover:underline">
              Kayıt olun
            </Link>
          </span>
        </CardFooter>
      </Card>
    </div>
  );
}

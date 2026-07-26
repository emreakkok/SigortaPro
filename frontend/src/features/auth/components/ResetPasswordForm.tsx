import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { useResetPassword } from "@/features/auth/hooks/useResetPassword";
import {
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from "@/features/auth/types/auth.schemas";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

interface ResetPasswordFormProps {
  /** URL query string'inden gelen e-posta ve token (form alanı değildir). */
  email: string;
  token: string;
}

export function ResetPasswordForm({ email, token }: ResetPasswordFormProps) {
  const navigate = useNavigate();
  const resetPasswordMutation = useResetPassword();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
  });

  const isLinkValid = email.length > 0 && token.length > 0;
  const serverErrors = resetPasswordMutation.isError
    ? getApiErrorMessages(resetPasswordMutation.error)
    : [];

  if (!isLinkValid) {
    return (
      <Alert variant="destructive">
        Şifre sıfırlama bağlantısı geçersiz veya eksik. Lütfen{" "}
        <Link to="/forgot-password" className="font-medium underline">
          yeni bir sıfırlama talebi
        </Link>{" "}
        oluşturun.
      </Alert>
    );
  }

  if (resetPasswordMutation.isSuccess) {
    return (
      <div className="space-y-4">
        <Alert>Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.</Alert>
        <Button className="w-full" onClick={() => navigate("/login", { replace: true })}>
          Giriş Yap
        </Button>
      </div>
    );
  }

  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={handleSubmit((values) =>
        resetPasswordMutation.mutate({ email, token, newPassword: values.newPassword }),
      )}
    >
      {serverErrors.length > 0 && (
        <Alert variant="destructive">
          <ul className="list-inside space-y-1">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </Alert>
      )}

      <FormField htmlFor="newPassword" label="Yeni Şifre" error={errors.newPassword?.message}>
        <Input
          id="newPassword"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
          {...register("newPassword")}
        />
      </FormField>

      <FormField
        htmlFor="confirmPassword"
        label="Yeni Şifre (Tekrar)"
        error={errors.confirmPassword?.message}
      >
        <Input
          id="confirmPassword"
          type="password"
          autoComplete="new-password"
          placeholder="••••••••"
          {...register("confirmPassword")}
        />
      </FormField>

      <Button type="submit" className="w-full" disabled={resetPasswordMutation.isPending}>
        {resetPasswordMutation.isPending ? (
          <Spinner className="[&>div]:h-4 [&>div]:w-4" />
        ) : (
          "Şifreyi Güncelle"
        )}
      </Button>
    </form>
  );
}

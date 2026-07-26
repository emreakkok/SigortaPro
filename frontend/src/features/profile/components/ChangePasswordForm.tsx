import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useChangePassword } from "@/features/profile/hooks/useProfile";
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
} from "@/features/profile/types/profile.schemas";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

/**
 * Şifre değiştirme formu (profil "Şifre Değiştir" sekmesi — ADR-040). Kurallar backend
 * ChangePasswordCommandValidator'ı aynalar; mevcut şifre doğrulaması backend'dedir (400).
 */
export function ChangePasswordForm() {
  const changePasswordMutation = useChangePassword();
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
  });

  const serverErrors = changePasswordMutation.isError
    ? getApiErrorMessages(changePasswordMutation.error)
    : [];

  return (
    <form
      className="max-w-md space-y-4"
      noValidate
      onSubmit={handleSubmit((values) =>
        changePasswordMutation.mutate(
          { currentPassword: values.currentPassword, newPassword: values.newPassword },
          { onSuccess: () => reset() },
        ),
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
      {changePasswordMutation.isSuccess && (
        <Alert variant="success">Şifreniz başarıyla değiştirildi.</Alert>
      )}

      <FormField htmlFor="currentPassword" label="Mevcut Şifre" error={errors.currentPassword?.message}>
        <Input
          id="currentPassword"
          type="password"
          autoComplete="current-password"
          placeholder="••••••••"
          {...register("currentPassword")}
        />
      </FormField>

      <FormField htmlFor="newPassword" label="Yeni Şifre" error={errors.newPassword?.message}>
        <Input
          id="newPassword"
          type="password"
          autoComplete="new-password"
          placeholder="En az 8 karakter; büyük/küçük harf, rakam ve özel karakter"
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

      <Button type="submit" disabled={changePasswordMutation.isPending}>
        {changePasswordMutation.isPending ? (
          <Spinner className="[&>div]:h-4 [&>div]:w-4" />
        ) : (
          "Şifreyi Değiştir"
        )}
      </Button>
    </form>
  );
}

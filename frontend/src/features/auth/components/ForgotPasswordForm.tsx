import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useForgotPassword } from "@/features/auth/hooks/useForgotPassword";
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "@/features/auth/types/auth.schemas";
import { Alert, Button, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

export function ForgotPasswordForm() {
  const forgotPasswordMutation = useForgotPassword();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const serverErrors = forgotPasswordMutation.isError
    ? getApiErrorMessages(forgotPasswordMutation.error)
    : [];

  // Güvenlik: başarı, e-postanın kayıtlı olduğunu göstermez (backend enumeration koruması).
  if (forgotPasswordMutation.isSuccess) {
    return (
      <Alert>
        Eğer bu e-posta adresi sistemimizde kayıtlıysa, şifre sıfırlama bağlantısını içeren bir e-posta
        gönderdik. Lütfen gelen kutunuzu (ve spam klasörünü) kontrol edin. Bağlantı 1 saat geçerlidir.
      </Alert>
    );
  }

  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={handleSubmit((values) => forgotPasswordMutation.mutate(values))}
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

      <FormField htmlFor="email" label="E-posta" error={errors.email?.message}>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="ornek@sigortapro.com"
          {...register("email")}
        />
      </FormField>

      <Button type="submit" className="w-full" disabled={forgotPasswordMutation.isPending}>
        {forgotPasswordMutation.isPending ? (
          <Spinner className="[&>div]:h-4 [&>div]:w-4" />
        ) : (
          "Sıfırlama Bağlantısı Gönder"
        )}
      </Button>
    </form>
  );
}

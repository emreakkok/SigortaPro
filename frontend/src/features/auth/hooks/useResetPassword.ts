import { useMutation } from "@tanstack/react-query";
import { resetPassword } from "@/features/auth/services/authApi";
import type { ResetPasswordRequest } from "@/features/auth/types/auth.types";

/**
 * Şifre sıfırlama mutation'ı. Başarıda form, giriş sayfasına yönlendiren bir onay durumu gösterir;
 * geçersiz/süresi dolmuş token → 400 (`{ errors }`) forma yansır.
 */
export function useResetPassword() {
  return useMutation({
    mutationFn: (request: ResetPasswordRequest) => resetPassword(request),
  });
}

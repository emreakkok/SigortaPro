import { useMutation } from "@tanstack/react-query";
import { forgotPassword } from "@/features/auth/services/authApi";
import type { ForgotPasswordRequest } from "@/features/auth/types/auth.types";

/**
 * Şifre sıfırlama talebi mutation'ı. Başarı, e-postanın kayıtlı olduğunu göstermez
 * (backend enumeration koruması); form generic bir onay mesajı gösterir.
 */
export function useForgotPassword() {
  return useMutation({
    mutationFn: (request: ForgotPasswordRequest) => forgotPassword(request),
  });
}

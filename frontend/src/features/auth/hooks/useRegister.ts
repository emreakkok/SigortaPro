import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { homePathFor, useAuth } from "@/features/auth/hooks/useAuth";
import { register } from "@/features/auth/services/authApi";
import type { RegisterRequest } from "@/features/auth/types/auth.types";

/** Kayıt mutation'ı: başarıda otomatik oturum açar (backend token döner) ve portala yönlendirir. */
export function useRegister() {
  const { signIn } = useAuth();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (request: RegisterRequest) => register(request),
    onSuccess: (auth) => {
      const session = signIn(auth);
      navigate(homePathFor(session), { replace: true });
    },
  });
}

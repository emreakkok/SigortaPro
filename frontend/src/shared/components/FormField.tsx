import type { ReactNode } from "react";
import { Label } from "@/shared/components/Label";

interface FormFieldProps {
  htmlFor: string;
  label: string;
  /** RHF alan hatası mesajı; varsa input altında gösterilir. */
  error?: string;
  children: ReactNode;
}

/** Label + kontrol + alan hatası üçlüsünü standartlaştıran form satırı. */
export function FormField({ htmlFor, label, error, children }: FormFieldProps) {
  return (
    <div className="space-y-2">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      {error !== undefined && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}

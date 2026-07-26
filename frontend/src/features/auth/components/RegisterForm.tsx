import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { useRegister } from "@/features/auth/hooks/useRegister";
import { registerSchema, type RegisterFormValues } from "@/features/auth/types/auth.schemas";
import { Alert, Button, CityCombobox, FormField, Input, Spinner } from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

export function RegisterForm() {
  const registerMutation = useRegister();
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  });

  const serverErrors = registerMutation.isError
    ? getApiErrorMessages(registerMutation.error)
    : [];

  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={handleSubmit((values) => registerMutation.mutate(values))}
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

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="firstName" label="Ad" error={errors.firstName?.message}>
          <Input id="firstName" autoComplete="given-name" {...register("firstName")} />
        </FormField>
        <FormField htmlFor="lastName" label="Soyad" error={errors.lastName?.message}>
          <Input id="lastName" autoComplete="family-name" {...register("lastName")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="tckn" label="TCKN" error={errors.tckn?.message}>
          <Input id="tckn" inputMode="numeric" maxLength={11} {...register("tckn")} />
        </FormField>
        <FormField htmlFor="birthDate" label="Doğum Tarihi" error={errors.birthDate?.message}>
          <Input id="birthDate" type="date" autoComplete="bday" {...register("birthDate")} />
        </FormField>
      </div>

      <FormField htmlFor="email" label="E-posta" error={errors.email?.message}>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder="ornek@sigortapro.com"
          {...register("email")}
        />
      </FormField>

      <FormField htmlFor="password" label="Şifre" error={errors.password?.message}>
        <Input
          id="password"
          type="password"
          autoComplete="new-password"
          placeholder="En az 8 karakter; büyük/küçük harf, rakam ve özel karakter"
          {...register("password")}
        />
      </FormField>

      <FormField
        htmlFor="phoneNumber"
        label="Telefon"
        error={errors.phoneNumber?.message}
      >
        <Input
          id="phoneNumber"
          type="tel"
          autoComplete="tel"
          placeholder="+905XXXXXXXXX"
          {...register("phoneNumber")}
        />
      </FormField>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="city" label="İl" error={errors.city?.message}>
          <Controller
            control={control}
            name="city"
            render={({ field }) => (
              <CityCombobox id="city" value={field.value ?? ""} onChange={field.onChange} />
            )}
          />
        </FormField>
        <FormField htmlFor="district" label="İlçe" error={errors.district?.message}>
          <Input id="district" {...register("district")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField
          htmlFor="neighborhood"
          label="Mahalle"
          error={errors.neighborhood?.message}
        >
          <Input id="neighborhood" {...register("neighborhood")} />
        </FormField>
        <FormField htmlFor="postalCode" label="Posta Kodu" error={errors.postalCode?.message}>
          <Input id="postalCode" inputMode="numeric" maxLength={10} {...register("postalCode")} />
        </FormField>
      </div>

      <Button type="submit" className="w-full" disabled={registerMutation.isPending}>
        {registerMutation.isPending ? (
          <Spinner className="[&>div]:h-4 [&>div]:w-4" />
        ) : (
          "Kayıt Ol"
        )}
      </Button>
    </form>
  );
}

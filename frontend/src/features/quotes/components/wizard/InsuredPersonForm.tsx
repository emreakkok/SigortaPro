import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  insuredPersonSchema,
  RELATIONSHIP_OPTIONS,
  type InsuredPersonFormValues,
} from "@/features/quotes/types/insuredPerson.schemas";
import { Button, FormField, Input, Select } from "@/shared/components";

interface InsuredPersonFormProps {
  defaultValues?: Partial<InsuredPersonFormValues>;
  onSubmit: (values: InsuredPersonFormValues) => void;
  submitLabel: string;
}

/**
 * "Başkası adına" sağlık sigortalısı beyan formu. Bilgiler poliçe sahibinin beyanıdır;
 * gizlilik gereği sistemdeki diğer müşteriler aranamaz/eşleştirilemez.
 */
export function InsuredPersonForm({ defaultValues, onSubmit, submitLabel }: InsuredPersonFormProps) {
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<InsuredPersonFormValues>({
    resolver: zodResolver(insuredPersonSchema),
    defaultValues,
  });

  // "Diğer" seçildiğinde serbest açıklama alanı açılır.
  const isOtherRelationship = watch("relationship") === "Diğer";

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit(onSubmit)}>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="insuredFirstName" label="Sigortalı Adı" error={errors.firstName?.message}>
          <Input id="insuredFirstName" autoComplete="off" {...register("firstName")} />
        </FormField>
        <FormField htmlFor="insuredLastName" label="Sigortalı Soyadı" error={errors.lastName?.message}>
          <Input id="insuredLastName" autoComplete="off" {...register("lastName")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="insuredTckn" label="TC Kimlik No" error={errors.tckn?.message}>
          <Input id="insuredTckn" inputMode="numeric" maxLength={11} autoComplete="off" {...register("tckn")} />
        </FormField>
        <FormField htmlFor="insuredBirthDate" label="Doğum Tarihi" error={errors.birthDate?.message}>
          <Input id="insuredBirthDate" type="date" {...register("birthDate")} />
        </FormField>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="insuredPhone" label="Telefon" error={errors.phoneNumber?.message}>
          <Input id="insuredPhone" type="tel" placeholder="+905XXXXXXXXX" {...register("phoneNumber")} />
        </FormField>
        <FormField htmlFor="insuredRelationship" label="Yakınlık Derecesi" error={errors.relationship?.message}>
          <Select id="insuredRelationship" defaultValue="" {...register("relationship")}>
            <option value="" disabled>
              Seçiniz
            </option>
            {RELATIONSHIP_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </Select>
        </FormField>
      </div>

      {isOtherRelationship && (
        <FormField
          htmlFor="insuredRelationshipDetail"
          label="Yakınlık Açıklaması"
          error={errors.relationshipDetail?.message}
        >
          <Input
            id="insuredRelationshipDetail"
            placeholder="ör. Kayınvalide, Torun"
            maxLength={50}
            {...register("relationshipDetail")}
          />
        </FormField>
      )}

      <Button type="submit">{submitLabel}</Button>
    </form>
  );
}

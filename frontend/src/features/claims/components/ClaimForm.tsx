import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  claimSchema,
  type ClaimFormValues,
} from "@/features/claims/types/claim.schemas";
import type { CreateClaimRequest } from "@/features/claims/types/claim.types";
import type { PolicyListItem } from "@/features/policies/types/policy.types";
import {
  Alert,
  Button,
  FormField,
  Input,
  Select,
  Spinner,
  Textarea,
} from "@/shared/components";
import { getApiErrorMessages } from "@/shared/lib/apiError";

interface ClaimFormProps {
  /** Yalnızca aktif poliçelere hasar açılabilir; seçenekler buradan gelir. */
  activePolicies: PolicyListItem[];
  onSubmit: (request: CreateClaimRequest) => void;
  isPending: boolean;
  error?: unknown;
}

const TODAY = new Date().toISOString().slice(0, 10);
const ACCEPTED_PHOTO_TYPES = ".jpg,.jpeg,.png,.pdf";
const MAX_PHOTOS = 10;

/** Hasar bildirim formu: poliçe seçimi, olay tarihi/açıklaması, tahmini tutar + mock foto metadatası. */
export function ClaimForm({ activePolicies, onSubmit, isPending, error }: ClaimFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ClaimFormValues>({
    resolver: zodResolver(claimSchema),
    defaultValues: { policyId: activePolicies.length === 1 ? activePolicies[0].id : "" },
  });

  // Foto yükleme mock'tur: gerçek dosya yüklenmez, yalnızca dosya adları metadata olarak gönderilir (ADR-024).
  const [photoNames, setPhotoNames] = useState<string[]>([]);
  const serverErrors = error !== undefined ? getApiErrorMessages(error) : [];

  const submit = (values: ClaimFormValues) =>
    onSubmit({
      policyId: values.policyId,
      incidentDate: values.incidentDate,
      description: values.description,
      estimatedAmount: values.estimatedAmount,
      photoFileNames: photoNames.length > 0 ? photoNames : undefined,
    });

  return (
    <form className="space-y-4" noValidate onSubmit={handleSubmit(submit)}>
      {serverErrors.length > 0 && (
        <Alert variant="destructive">
          <ul className="list-inside space-y-1">
            {serverErrors.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
        </Alert>
      )}

      <FormField htmlFor="policyId" label="Poliçe" error={errors.policyId?.message}>
        <Select id="policyId" defaultValue={activePolicies.length === 1 ? activePolicies[0].id : ""} {...register("policyId")}>
          <option value="" disabled>
            Poliçe seçiniz
          </option>
          {activePolicies.map((policy) => (
            <option key={policy.id} value={policy.id}>
              {policy.policyNumber} · {policy.productName}
            </option>
          ))}
        </Select>
      </FormField>

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField htmlFor="incidentDate" label="Olay Tarihi" error={errors.incidentDate?.message}>
          <Input id="incidentDate" type="date" max={TODAY} {...register("incidentDate")} />
        </FormField>
        <FormField
          htmlFor="estimatedAmount"
          label="Tahmini Hasar Tutarı (₺)"
          error={errors.estimatedAmount?.message}
        >
          <Input id="estimatedAmount" type="number" inputMode="decimal" step="0.01" {...register("estimatedAmount")} />
        </FormField>
      </div>

      <FormField htmlFor="description" label="Hasar Açıklaması" error={errors.description?.message}>
        <Textarea
          id="description"
          rows={4}
          placeholder="Olayı ve hasarı kısaca açıklayın."
          {...register("description")}
        />
      </FormField>

      <div className="space-y-2">
        <label htmlFor="photos" className="text-sm font-medium">
          Fotoğraflar <span className="text-muted-foreground">(opsiyonel, demo)</span>
        </label>
        <input
          id="photos"
          type="file"
          multiple
          accept={ACCEPTED_PHOTO_TYPES}
          className="block w-full text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-2 file:text-sm file:font-medium hover:file:bg-accent"
          onChange={(event) => {
            const names = Array.from(event.target.files ?? [])
              .slice(0, MAX_PHOTOS)
              .map((file) => file.name);
            setPhotoNames(names);
          }}
        />
        {photoNames.length > 0 && (
          <p className="text-xs text-muted-foreground">
            Seçilen: {photoNames.join(", ")} — bu demoda fotoğraflar yüklenmez, yalnızca dosya adları kaydedilir.
          </p>
        )}
      </div>

      <Button type="submit" disabled={isPending}>
        {isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Hasar Bildir"}
      </Button>
    </form>
  );
}

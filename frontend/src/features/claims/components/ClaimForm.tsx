import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import {
  claimSchema,
  combineIncidentDateTime,
  type ClaimFormValues,
} from "@/features/claims/types/claim.schemas";
import type {
  CreateClaimDocumentPayload,
  CreateClaimRequest,
} from "@/features/claims/types/claim.types";
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

const ACCEPTED_DOCUMENT_TYPES = ".jpg,.jpeg,.png,.webp,.pdf";
const MAX_DOCUMENTS = 5;
const MAX_DOCUMENT_SIZE_BYTES = 3 * 1024 * 1024; // 3 MB (backend ile aynı sınır)

/** Seçilen dosyayı backend'in beklediği base64 içerikli belge yüküne çevirir. */
async function fileToPayload(file: File): Promise<CreateClaimDocumentPayload> {
  const buffer = new Uint8Array(await file.arrayBuffer());
  let binary = "";
  for (let index = 0; index < buffer.length; index += 1) {
    binary += String.fromCharCode(buffer[index]);
  }
  return {
    fileName: file.name,
    contentType: file.type || "application/octet-stream",
    content: btoa(binary),
  };
}

/** Bugünün yerel tarihi ("YYYY-MM-DD") — gelecek tarih seçimini engellemek için `max`. */
function localDateValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Şimdinin yerel saati ("HH:mm") — saat alanı için makul varsayılan. */
function localTimeValue(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Hasar bildirim formu: poliçe seçimi, olay tarihi/açıklaması, tahmini tutar + mock foto metadatası. */
export function ClaimForm({ activePolicies, onSubmit, isPending, error }: ClaimFormProps) {
  const now = new Date();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ClaimFormValues>({
    resolver: zodResolver(claimSchema),
    defaultValues: {
      policyId: activePolicies.length === 1 ? activePolicies[0].id : "",
      incidentDate: localDateValue(now),
      incidentTime: localTimeValue(now),
    },
  });

  // Belgeler (foto/PDF) gerçek olarak yüklenir: seçilen dosyalar base64'e çevrilip gönderilir, backend
  // depolamada saklar ve Admin/Personel değerlendirmede görür. Baytlar submit anında okunur.
  const [files, setFiles] = useState<File[]>([]);
  const [fileError, setFileError] = useState<string | null>(null);
  const serverErrors = error !== undefined ? getApiErrorMessages(error) : [];
  const today = localDateValue(now);

  const submit = async (values: ClaimFormValues) => {
    const documents = files.length > 0 ? await Promise.all(files.map(fileToPayload)) : undefined;
    onSubmit({
      policyId: values.policyId,
      // Olay tarihi + saati birleştirilip UTC ISO'ya çevrilir; backend teminat penceresini bu kesin anla
      // (saat dahil) karşılaştırır — böylece aynı gün poliçe başlangıcından sonraki hasar doğru kabul edilir.
      incidentDate: combineIncidentDateTime(values.incidentDate, values.incidentTime).toISOString(),
      description: values.description,
      estimatedAmount: values.estimatedAmount,
      documents,
    });
  };

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
          <Input id="incidentDate" type="date" max={today} {...register("incidentDate")} />
        </FormField>
        <FormField htmlFor="incidentTime" label="Olay Saati" error={errors.incidentTime?.message}>
          <Input id="incidentTime" type="time" {...register("incidentTime")} />
        </FormField>
      </div>

      <FormField
        htmlFor="estimatedAmount"
        label="Tahmini Hasar Tutarı (₺)"
        error={errors.estimatedAmount?.message}
      >
        <Input id="estimatedAmount" type="number" inputMode="decimal" step="0.01" {...register("estimatedAmount")} />
      </FormField>

      <FormField htmlFor="description" label="Hasar Açıklaması" error={errors.description?.message}>
        <Textarea
          id="description"
          rows={4}
          placeholder="Olayı ve hasarı kısaca açıklayın."
          {...register("description")}
        />
      </FormField>

      <div className="space-y-2">
        <label htmlFor="documents" className="text-sm font-medium">
          Belgeler / Fotoğraflar <span className="text-muted-foreground">(opsiyonel)</span>
        </label>
        <input
          id="documents"
          type="file"
          multiple
          accept={ACCEPTED_DOCUMENT_TYPES}
          className="block w-full text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-2 file:text-sm file:font-medium hover:file:bg-accent"
          onChange={(event) => {
            const selected = Array.from(event.target.files ?? []);
            const withinSize = selected.filter((file) => file.size <= MAX_DOCUMENT_SIZE_BYTES);
            const skipped = selected.length - withinSize.length;
            setFiles(withinSize.slice(0, MAX_DOCUMENTS));
            setFileError(
              skipped > 0 ? `${skipped} dosya 3 MB sınırını aştığı için eklenmedi.` : null,
            );
          }}
        />
        <p className="text-xs text-muted-foreground">
          JPEG, PNG, WEBP veya PDF · en fazla {MAX_DOCUMENTS} dosya · dosya başına 3 MB. Yüklediğiniz
          belgeler hasar değerlendirmesinde acente tarafından görüntülenir.
        </p>
        {fileError !== null && <p className="text-xs text-destructive">{fileError}</p>}
        {files.length > 0 && (
          <p className="text-xs text-muted-foreground">Seçilen: {files.map((file) => file.name).join(", ")}</p>
        )}
      </div>

      <Button type="submit" disabled={isPending}>
        {isPending ? <Spinner className="[&>div]:h-4 [&>div]:w-4" /> : "Hasar Bildir"}
      </Button>
    </form>
  );
}

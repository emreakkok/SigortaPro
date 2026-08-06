import { useCityCatalog } from "@/shared/hooks/useCityCatalog";
import { Combobox } from "@/shared/components/Combobox";
import { Input } from "@/shared/components/Input";

interface CityComboboxProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

/**
 * Adres formlarındaki "İl" alanı için aranabilir il seçici. 81 ili katalogdan çeker ve shared
 * Combobox ile sunar. Katalog yüklenemezse (ağ hatası) serbest metin girişine düşülür; böylece form
 * her koşulda çalışır. RHF Controller ile `value`/`onChange` üzerinden bağlanır — form sözleşmesi (string) değişmez.
 */
export function CityCombobox({ id, value, onChange, disabled = false }: CityComboboxProps) {
  const { data, isError } = useCityCatalog();
  const cityNames = data?.cities.map((city) => city.name) ?? [];

  if (isError) {
    return (
      <Input
        id={id}
        value={value}
        disabled={disabled}
        placeholder="İl"
        onChange={(event) => onChange(event.target.value)}
      />
    );
  }

  return (
    <Combobox
      id={id}
      value={value}
      onChange={onChange}
      options={cityNames}
      disabled={disabled}
      placeholder="İl seçin veya arayın"
      emptyText="İl bulunamadı."
    />
  );
}

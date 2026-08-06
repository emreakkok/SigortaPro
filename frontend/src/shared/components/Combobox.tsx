import { useEffect, useRef, useState } from "react";
import { cn } from "@/shared/lib/utils";

interface ComboboxProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder?: string;
  disabled?: boolean;
  emptyText?: string;
}

/**
 * El yazımı, aranabilir combobox.
 * Değer yalnızca listeden seçim ile commit edilir; serbest metin girişi için VehicleForm'daki
 * "Diğer" düğmesi (native input) kullanılır. Klavye: ↑/↓ gezinme, Enter seçim, Esc kapatma.
 */
export function Combobox({
  id,
  value,
  onChange,
  options,
  placeholder,
  disabled = false,
  emptyText = "Sonuç bulunamadı.",
}: ComboboxProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [highlight, setHighlight] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);

  const normalizedQuery = query.trim().toLowerCase();
  const filtered =
    normalizedQuery === ""
      ? options
      : options.filter((option) => option.toLowerCase().includes(normalizedQuery));

  useEffect(() => {
    function handleOutsideClick(event: MouseEvent) {
      if (containerRef.current !== null && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutsideClick);
    return () => document.removeEventListener("mousedown", handleOutsideClick);
  }, []);

  function commit(option: string) {
    onChange(option);
    setQuery(option);
    setOpen(false);
  }

  return (
    <div ref={containerRef} className="relative">
      <input
        id={id}
        type="text"
        role="combobox"
        aria-expanded={open}
        aria-autocomplete="list"
        autoComplete="off"
        disabled={disabled}
        value={open ? query : value}
        placeholder={placeholder}
        onFocus={() => {
          setQuery(value);
          setHighlight(0);
          setOpen(true);
        }}
        onChange={(event) => {
          setQuery(event.target.value);
          setHighlight(0);
          setOpen(true);
        }}
        onKeyDown={(event) => {
          if (event.key === "ArrowDown") {
            event.preventDefault();
            setOpen(true);
            setHighlight((current) => Math.min(current + 1, Math.max(filtered.length - 1, 0)));
          } else if (event.key === "ArrowUp") {
            event.preventDefault();
            setHighlight((current) => Math.max(current - 1, 0));
          } else if (event.key === "Enter") {
            if (open && filtered[highlight] !== undefined) {
              event.preventDefault();
              commit(filtered[highlight]);
            }
          } else if (event.key === "Escape") {
            setOpen(false);
          }
        }}
        className={cn(
          "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
        )}
      />

      {open && (
        <ul
          role="listbox"
          className="absolute z-20 mt-1 max-h-56 w-full overflow-auto rounded-md border border-input bg-background py-1 text-sm shadow-md"
        >
          {filtered.length === 0 ? (
            <li className="px-3 py-2 text-muted-foreground">{emptyText}</li>
          ) : (
            filtered.map((option, index) => (
              <li
                key={option}
                role="option"
                aria-selected={option === value}
                // onMouseDown + preventDefault: input blur'undan önce seçimi yakalar (dropdown erken kapanmaz).
                onMouseDown={(event) => {
                  event.preventDefault();
                  commit(option);
                }}
                onMouseEnter={() => setHighlight(index)}
                className={cn(
                  "cursor-pointer px-3 py-2",
                  index === highlight && "bg-accent",
                  option === value && "font-medium",
                )}
              >
                {option}
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
}

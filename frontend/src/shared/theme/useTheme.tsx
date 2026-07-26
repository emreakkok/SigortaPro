import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

/**
 * Merkezi tema yönetimi (ADR-043). Renk kimliği tamamen token tabanlıdır (globals.css `.dark`);
 * bu katman yalnızca hangi temanın etkin olduğunu belirler ve `.dark` sınıfını <html> üzerine uygular.
 * Üç mod desteklenir: açık (light), koyu (dark) ve sistem (system → işletim sistemini izler).
 * Tercih localStorage'da saklanır; ilk boyama öncesi index.html içindeki inline script ile uygulanır (FOUC yok).
 */
export type ThemeMode = "light" | "dark" | "system";
export type EffectiveTheme = "light" | "dark";

/** index.html'deki FOUC-önleme scripti ile birebir aynı olmalıdır. */
const STORAGE_KEY = "sigortapro.theme";
const MODE_CYCLE: ThemeMode[] = ["light", "dark", "system"];

interface ThemeContextValue {
  /** Kullanıcının seçtiği mod (system dahil). */
  mode: ThemeMode;
  /** Ekrana gerçekten uygulanan tema (system → çözümlenmiş light/dark). */
  effectiveTheme: EffectiveTheme;
  setMode: (mode: ThemeMode) => void;
  /** light → dark → system → light sırayla döner. */
  cycleMode: () => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

function prefersDark(): boolean {
  return (
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-color-scheme: dark)").matches
  );
}

function readStoredMode(): ThemeMode {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw === "light" || raw === "dark" || raw === "system") {
      return raw;
    }
  } catch {
    /* localStorage erişilemezse (gizli mod vb.) sistem tercihine düşülür */
  }
  return "system";
}

function resolveEffective(mode: ThemeMode): EffectiveTheme {
  if (mode === "system") {
    return prefersDark() ? "dark" : "light";
  }
  return mode;
}

/** `.dark` sınıfını ve native form/scrollbar'ları etkileyen color-scheme'i uygular. */
function applyEffective(effective: EffectiveTheme): void {
  const root = document.documentElement;
  root.classList.toggle("dark", effective === "dark");
  root.style.colorScheme = effective;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(() => readStoredMode());
  const [effectiveTheme, setEffectiveTheme] = useState<EffectiveTheme>(() =>
    resolveEffective(readStoredMode()),
  );

  // Mod değiştiğinde: etkin temayı uygula ve tercihi kalıcı sakla.
  useEffect(() => {
    const effective = resolveEffective(mode);
    setEffectiveTheme(effective);
    applyEffective(effective);
    try {
      localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      /* kalıcılık yoksa oturum içi çalışmaya devam edilir */
    }
  }, [mode]);

  // Yalnızca "system" modunda işletim sistemi teması değişimini canlı izle.
  useEffect(() => {
    if (mode !== "system") {
      return;
    }
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const handleChange = () => {
      const effective: EffectiveTheme = media.matches ? "dark" : "light";
      setEffectiveTheme(effective);
      applyEffective(effective);
    };
    media.addEventListener("change", handleChange);
    return () => media.removeEventListener("change", handleChange);
  }, [mode]);

  const setMode = useCallback((next: ThemeMode) => setModeState(next), []);
  const cycleMode = useCallback(() => {
    setModeState((current) => MODE_CYCLE[(MODE_CYCLE.indexOf(current) + 1) % MODE_CYCLE.length]);
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ mode, effectiveTheme, setMode, cycleMode }),
    [mode, effectiveTheme, setMode, cycleMode],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (context === null) {
    throw new Error("useTheme, ThemeProvider içinde kullanılmalıdır.");
  }
  return context;
}

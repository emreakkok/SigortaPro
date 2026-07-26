import type { SVGProps } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import { MonitorIcon, MoonIcon, SunIcon } from "@/shared/components";
import { cn } from "@/shared/lib/utils";
import { useTheme, type ThemeMode } from "@/shared/theme/useTheme";

/**
 * Navbar'daki tema değiştirme butonu (ADR-043). Tek tıkta açık → koyu → sistem sırayla döner;
 * ikon Framer Motion ile hafifçe (opacity + rotate + scale) çapraz geçiş yapar (`useReducedMotion`'a saygılı).
 * Renk yükü yok — token tabanlı `text-muted-foreground/accent` kullanır, koyu temada otomatik uyumlu.
 */
const MODE_META: Record<
  ThemeMode,
  { label: string; Icon: (props: SVGProps<SVGSVGElement>) => JSX.Element }
> = {
  light: { label: "Açık tema", Icon: SunIcon },
  dark: { label: "Koyu tema", Icon: MoonIcon },
  system: { label: "Sistem teması", Icon: MonitorIcon },
};

export function ThemeToggle({ className }: { className?: string }) {
  const { mode, cycleMode } = useTheme();
  const reduceMotion = useReducedMotion();
  const meta = MODE_META[mode];
  const Icon = meta.Icon;

  return (
    <button
      type="button"
      onClick={cycleMode}
      aria-label={`Tema: ${meta.label}. Değiştirmek için tıklayın.`}
      title={`Tema: ${meta.label}`}
      className={cn(
        "relative flex h-9 w-9 items-center justify-center overflow-hidden rounded-full text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
    >
      <AnimatePresence initial={false} mode="wait">
        <motion.span
          key={mode}
          initial={reduceMotion ? false : { opacity: 0, rotate: -90, scale: 0.6 }}
          animate={{ opacity: 1, rotate: 0, scale: 1 }}
          exit={reduceMotion ? { opacity: 0 } : { opacity: 0, rotate: 90, scale: 0.6 }}
          transition={{ duration: reduceMotion ? 0 : 0.2, ease: "easeOut" }}
          className="flex items-center justify-center"
        >
          <Icon className="h-[1.15rem] w-[1.15rem]" />
        </motion.span>
      </AnimatePresence>
    </button>
  );
}

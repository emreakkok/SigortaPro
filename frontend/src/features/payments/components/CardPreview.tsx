import type { ReactNode } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { CardBrandLogo } from "@/features/payments/components/CardBrandLogo";
import {
  detectCardBrand,
  formatCardNumberDisplay,
  formatCvvDisplay,
  formatExpiryDisplay,
  formatHolderDisplay,
} from "@/features/payments/utils/card";
import { cn } from "@/shared/lib/utils";

/** Kart görselinde vurgulanabilen alanlar (form alanlarının görsel karşılığı). */
export type CardField = "cardNumber" | "cardHolderName" | "expiry" | "cvv";

interface CardPreviewProps {
  cardNumber: string;
  cardHolderName: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
  /** Odaktaki alan: CVV ise kart arka yüze döner, diğerlerinde ilgili bölge vurgulanır. */
  focusedField: CardField | null;
}

/**
 * Ödeme formunun canlı kart önizlemesi. Değerler RHF `watch` ile gelir; bileşen kendi durumunu tutmaz
 * (yalnızca sunum). CVV alanına odaklanıldığında kart 3D olarak arka yüzüne döner.
 * Görsel bir aynadır; ekran okuyucular için kaynak doğruluk formun kendisidir → aria-hidden.
 */
export function CardPreview({
  cardNumber,
  cardHolderName,
  expiryMonth,
  expiryYear,
  cvv,
  focusedField,
}: CardPreviewProps) {
  const reduceMotion = useReducedMotion();
  const brand = detectCardBrand(cardNumber);
  const isFlipped = focusedField === "cvv";

  return (
    <div aria-hidden="true" className="mx-auto w-full max-w-sm [perspective:1400px]">
      <motion.div
        className="relative aspect-[1.586] w-full [transform-style:preserve-3d]"
        animate={{ rotateY: isFlipped ? 180 : 0 }}
        transition={
          reduceMotion ? { duration: 0 } : { duration: 0.55, ease: [0.22, 1, 0.36, 1] }
        }
        whileHover={reduceMotion ? undefined : { scale: 1.015, rotateX: 4 }}
      >
        <CardFace>
          <div className="relative flex h-full flex-col justify-between p-5 sm:p-6">
            <div className="flex items-start justify-between">
              <Chip />
              <CardBrandLogo brand={brand} />
            </div>

            <Highlight active={focusedField === "cardNumber"}>
              <p className="font-mono text-base tracking-[0.12em] sm:text-xl sm:tracking-[0.15em]">
                {formatCardNumberDisplay(cardNumber)}
              </p>
            </Highlight>

            <div className="flex items-end justify-between gap-3">
              <Highlight active={focusedField === "cardHolderName"} className="min-w-0 flex-1">
                <FieldLabel>Kart Sahibi</FieldLabel>
                <p className="truncate text-sm font-medium">{formatHolderDisplay(cardHolderName)}</p>
              </Highlight>

              <Highlight active={focusedField === "expiry"}>
                <FieldLabel>Son Kul.</FieldLabel>
                <p className="font-mono text-sm">{formatExpiryDisplay(expiryMonth, expiryYear)}</p>
              </Highlight>
            </div>
          </div>
        </CardFace>

        <CardFace className="[transform:rotateY(180deg)]">
          <div className="relative flex h-full flex-col">
            {/* Manyetik şerit */}
            <div className="mt-5 h-10 w-full bg-slate-950/85" />

            <div className="flex flex-1 flex-col justify-between p-5 sm:p-6">
              <div className="flex items-center gap-3">
                {/* İmza alanı */}
                <div className="h-9 flex-1 rounded-sm bg-gradient-to-r from-white/85 to-white/70" />
                <Highlight active={focusedField === "cvv"}>
                  <FieldLabel>CVV</FieldLabel>
                  <p className="font-mono text-sm">{formatCvvDisplay(cvv)}</p>
                </Highlight>
              </div>

              <div className="flex items-end justify-between">
                <p className="text-[10px] tracking-wide text-white/50">SigortaPro</p>
                <CardBrandLogo brand={brand} />
              </div>
            </div>
          </div>
        </CardFace>
      </motion.div>
    </div>
  );
}

/**
 * Kartın bir yüzü: gradient + cam (glassmorphism) katmanları ve derinlik gölgesi.
 * Renkler tema token'larından (`primary`) türer → koyu tema otomatik uyumludur (ADR-027).
 */
function CardFace({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div
      className={cn(
        "absolute inset-0 overflow-hidden rounded-2xl border border-white/15 text-white shadow-2xl shadow-primary/25 [backface-visibility:hidden]",
        "bg-gradient-to-br from-primary via-primary/75 to-slate-900",
        className,
      )}
    >
      {/* Cam efekti: yumuşak ışık lekeleri + ince parlama katmanı (statik → animasyon maliyeti yok). */}
      <div className="pointer-events-none absolute -left-12 -top-16 h-40 w-40 rounded-full bg-white/25 blur-2xl" />
      <div className="pointer-events-none absolute -bottom-16 -right-10 h-44 w-44 rounded-full bg-white/10 blur-2xl" />
      <div className="pointer-events-none absolute inset-0 bg-gradient-to-tr from-transparent via-white/5 to-white/15" />
      {children}
    </div>
  );
}

/** Odaktaki form alanının kart üzerindeki karşılığını hafifçe vurgular. */
function Highlight({
  active,
  children,
  className,
}: {
  active: boolean;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "rounded-lg px-2 py-1 ring-1 ring-inset transition-colors duration-200",
        active ? "bg-white/15 ring-white/50" : "ring-transparent",
        className,
      )}
    >
      {children}
    </div>
  );
}

function FieldLabel({ children }: { children: ReactNode }) {
  return (
    <p className="text-[10px] uppercase tracking-widest text-white/60">{children}</p>
  );
}

/** EMV çip görseli. */
function Chip() {
  return (
    <div className="h-8 w-11 overflow-hidden rounded-md bg-gradient-to-br from-amber-200 via-yellow-400 to-amber-500 shadow-inner">
      <div className="h-full w-full bg-[linear-gradient(90deg,transparent_45%,rgba(0,0,0,0.25)_45%,rgba(0,0,0,0.25)_55%,transparent_55%),linear-gradient(0deg,transparent_45%,rgba(0,0,0,0.25)_45%,rgba(0,0,0,0.25)_55%,transparent_55%)]" />
    </div>
  );
}

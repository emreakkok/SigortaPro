import { AnimatePresence, motion } from "framer-motion";
import type { CardBrand } from "@/features/payments/utils/card";
import { cn } from "@/shared/lib/utils";

interface CardBrandLogoProps {
  brand: CardBrand;
  className?: string;
}

/**
 * Kart markası rozeti — tamamen projeye özel, inline SVG/tipografi ile çizilir (hazır kart kütüphanesi
 * kullanılmaz). Marka değiştiğinde rozet yumuşak bir geçişle güncellenir; algılanamayan kartlarda nötr
 * görünüm gösterilir (yükseklik sabit kalır → kart düzeni zıplamaz).
 */
export function CardBrandLogo({ brand, className }: CardBrandLogoProps) {
  return (
    <div className={cn("flex h-7 min-w-[3.5rem] items-center justify-end", className)}>
      <AnimatePresence mode="wait" initial={false}>
        <motion.div
          key={brand}
          initial={{ opacity: 0, scale: 0.85 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.85 }}
          transition={{ duration: 0.18, ease: "easeOut" }}
          className="flex items-center justify-end"
        >
          <BrandMark brand={brand} />
        </motion.div>
      </AnimatePresence>
    </div>
  );
}

function BrandMark({ brand }: { brand: CardBrand }) {
  if (brand === "visa") {
    return (
      <span className="select-none text-xl font-black italic tracking-tight text-white drop-shadow-sm">
        VISA
      </span>
    );
  }

  if (brand === "mastercard") {
    // Klasik iç içe geçmiş iki daire; ortadaki kesişim turuncu ile vurgulanır.
    return (
      <svg viewBox="0 0 48 30" className="h-7 w-auto" aria-hidden="true" focusable="false">
        <circle cx="19" cy="15" r="11" fill="#EB001B" />
        <circle cx="29" cy="15" r="11" fill="#F79E1B" />
        <path
          d="M24 6.6a11 11 0 0 0 0 16.8 11 11 0 0 0 0-16.8Z"
          fill="#FF5F00"
        />
      </svg>
    );
  }

  if (brand === "troy") {
    return (
      <span className="flex select-none items-center gap-1">
        <span className="text-lg font-extrabold lowercase tracking-tight text-white drop-shadow-sm">
          troy
        </span>
        <span className="h-2 w-2 rounded-full bg-gradient-to-br from-sky-300 to-emerald-300" />
      </span>
    );
  }

  // Nötr: marka algılanmadı — sade, dikkat çekmeyen yer tutucu.
  return (
    <span className="select-none font-mono text-xs tracking-[0.3em] text-white/40">••••</span>
  );
}

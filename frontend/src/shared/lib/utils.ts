import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Koşullu Tailwind sınıflarını birleştirir ve çakışanları çözer
 * (shadcn/ui `cn` konvansiyonu — tüm bileşenler bunu kullanır).
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

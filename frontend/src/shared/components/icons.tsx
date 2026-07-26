import type { SVGProps } from "react";

/**
 * Küçük, el yazımı inline SVG ikon seti (ADR-027 konvansiyonu — ikon kütüphanesi eklenmez).
 * Tümü stroke tabanlı ve `currentColor` kullanır → metin rengiyle/temayla otomatik uyumlu.
 * Nav ve menü gibi tekrar eden yüzeylerde metin yükünü azaltmak için kullanılır (ADR-039).
 */
type IconProps = SVGProps<SVGSVGElement>;

function baseProps(props: IconProps): IconProps {
  return {
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
    focusable: false,
    className: "h-4 w-4 shrink-0",
    ...props,
  };
}

export function HomeIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 9.5V21h14V9.5" />
      <path d="M9.5 21v-6h5v6" />
    </svg>
  );
}

export function FileTextIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M14 3H6.5A1.5 1.5 0 0 0 5 4.5v15A1.5 1.5 0 0 0 6.5 21h11a1.5 1.5 0 0 0 1.5-1.5V8z" />
      <path d="M14 3v5h5" />
      <path d="M8.5 12.5h7M8.5 16h7" />
    </svg>
  );
}

export function ShieldIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M12 3 5 5.8v5.4c0 4.3 2.9 8 7 9.8 4.1-1.8 7-5.5 7-9.8V5.8z" />
    </svg>
  );
}

export function ShieldCheckIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M12 3 5 5.8v5.4c0 4.3 2.9 8 7 9.8 4.1-1.8 7-5.5 7-9.8V5.8z" />
      <path d="m9 12 2.2 2.2L15.5 10" />
    </svg>
  );
}

export function AlertTriangleIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M12 4 2.8 19.5h18.4z" />
      <path d="M12 10v4.5" />
      <path d="M12 17.4v.1" />
    </svg>
  );
}

export function RefreshIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M20 11a8 8 0 0 0-14.9-3M4 13a8 8 0 0 0 14.9 3" />
      <path d="M5 4v4h4M19 20v-4h-4" />
    </svg>
  );
}

export function UserIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <circle cx="12" cy="8" r="3.5" />
      <path d="M4.5 20.2a7.5 7.5 0 0 1 15 0" />
    </svg>
  );
}

export function LogoutIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M14 4H6.5A1.5 1.5 0 0 0 5 5.5v13A1.5 1.5 0 0 0 6.5 20H14" />
      <path d="M10 12h10M17 8.5 20.5 12 17 15.5" />
    </svg>
  );
}

export function BellIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M18 10a6 6 0 1 0-12 0c0 4-1.5 5.5-2.5 6.5h17C19.5 15.5 18 14 18 10z" />
      <path d="M10 20a2.2 2.2 0 0 0 4 0" />
    </svg>
  );
}

export function HeartIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M12 20s-7-4.5-9-9a4.8 4.8 0 0 1 8.5-3.6L12 8l.5-.6A4.8 4.8 0 0 1 21 11c-2 4.5-9 9-9 9z" />
    </svg>
  );
}

export function UsersIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <circle cx="9" cy="8.5" r="3" />
      <path d="M3 19.5a6 6 0 0 1 12 0" />
      <path d="M16 6.2a3 3 0 0 1 0 4.6M17.5 13.6a6 6 0 0 1 3.5 5.4" />
    </svg>
  );
}

export function ChartIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M4 4v16h16" />
      <path d="M8.5 15.5v-4M13 15.5V8M17.5 15.5v-6.5" />
    </svg>
  );
}

export function SunIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
    </svg>
  );
}

export function MoonIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <path d="M20 14.5A8 8 0 0 1 9.5 4a7 7 0 1 0 10.5 10.5z" />
    </svg>
  );
}

export function MonitorIcon(props: IconProps) {
  return (
    <svg {...baseProps(props)}>
      <rect x="3" y="4" width="18" height="12" rx="1.5" />
      <path d="M8 20h8M12 16v4" />
    </svg>
  );
}

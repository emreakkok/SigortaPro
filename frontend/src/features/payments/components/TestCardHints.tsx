import { Alert } from "@/shared/components";

/** Mock sanal POS test kartları (backend README + ADR-007). Gerçek kart kullanılmaz. */
const TEST_CARDS = [
  { number: "4111 1111 1111 1111", result: "Başarılı ödeme" },
  { number: "4000 0000 0000 0002", result: "Yetersiz bakiye (başarısız)" },
  { number: "4000 0000 0000 0069", result: "3D Secure hatası (başarısız)" },
];

/** Ödeme sayfasında gösterilen test kartı ipuçları. */
export function TestCardHints() {
  return (
    <Alert>
      <p className="font-medium">Bu bir demo ödeme ekranıdır — gerçek kart bilgisi girmeyin.</p>
      <p className="mt-1 text-muted-foreground">
        Aşağıdaki test kartlarıyla farklı senaryoları deneyebilirsiniz (son kullanma tarihi gelecekte
        herhangi bir değer, CVV 3 hane):
      </p>
      <ul className="mt-2 space-y-1">
        {TEST_CARDS.map((card) => (
          <li key={card.number} className="flex flex-wrap justify-between gap-2">
            <span className="font-mono">{card.number}</span>
            <span className="text-muted-foreground">{card.result}</span>
          </li>
        ))}
      </ul>
    </Alert>
  );
}

import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { motion, useReducedMotion } from "framer-motion";
import {
  AlertTriangleIcon,
  Button,
  Card,
  CardContent,
  ChartIcon,
  FileTextIcon,
  HeartIcon,
  HomeIcon,
  RefreshIcon,
  ShieldCheckIcon,
  ShieldIcon,
} from "@/shared/components";
import { ThemeToggle } from "@/shared/theme/ThemeToggle";
// Branşa özel, tema (koyu + bordo) ile birebir uyumlu özgün SVG illüstrasyonları — yerel asset (harici
// hotlink yok; self-authored → telif temiz). Aynı tasarım dili tüm kartlarda tutarlı görünürlük sağlar.
import kaskoImage from "@/assets/services/kasko.svg";
import trafikImage from "@/assets/services/trafik.svg";
import konutImage from "@/assets/services/konut.svg";
import daskImage from "@/assets/services/dask.svg";
import saglikImage from "@/assets/services/saglik.svg";

/**
 * Giriş yapmamış kullanıcının karşılama sayfası (ADR-039). Modern SaaS düzeni: hero, hizmetler,
 * nasıl çalışır, avantajlar, SSS, CTA ve footer. Tasarım sistemi token'larıyla (ADR-027) çizilir →
 * koyu tema otomatik uyumlu. Framer Motion yalnızca hafif giriş animasyonlarında kullanılır
 * (transform/opacity; `useReducedMotion`'a saygılı).
 */
export default function LandingPage() {
  return (
    <div className="flex min-h-screen flex-col bg-background">
      <LandingHeader />
      <main className="flex-1">
        <HeroSection />
        <ServicesSection />
        <HowItWorksSection />
        <AdvantagesSection />
        <FaqSection />
        <CtaSection />
      </main>
      <LandingFooter />
    </div>
  );
}

function LandingHeader() {
  return (
    <header className="sticky top-0 z-20 border-b bg-background/80 backdrop-blur">
      <div className="container flex h-16 items-center justify-between">
        <span className="flex items-center gap-2 text-xl font-bold text-primary">
          <ShieldIcon className="h-6 w-6" />
          SigortaPro
        </span>
        <nav className="flex items-center gap-2">
          <ThemeToggle />
          <Link to="/login">
            <Button variant="ghost" size="sm">
              Giriş Yap
            </Button>
          </Link>
          <Link to="/register">
            <Button size="sm">Hemen Başla</Button>
          </Link>
        </nav>
      </div>
    </header>
  );
}

/** Bölümlerin viewport'a girişte bir kez, hafifçe belirmesi (ağır scroll animasyonu yok). */
function Reveal({ children, delay = 0 }: { children: ReactNode; delay?: number }) {
  const reduceMotion = useReducedMotion();
  if (reduceMotion) {
    return <>{children}</>;
  }
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-40px" }}
      transition={{ duration: 0.45, delay, ease: "easeOut" }}
    >
      {children}
    </motion.div>
  );
}

function HeroSection() {
  return (
    <section className="relative overflow-hidden">
      {/* Yumuşak arka plan vurguları (statik — animasyon maliyeti yok). */}
      <div className="pointer-events-none absolute -top-32 left-1/2 h-96 w-[42rem] -translate-x-1/2 rounded-full bg-primary/10 blur-3xl" />
      <div className="container flex flex-col items-center gap-6 py-20 text-center sm:py-28">
        <Reveal>
          <span className="rounded-full border border-primary/30 bg-accent px-4 py-1 text-sm font-medium text-accent-foreground">
            Dijital sigorta acenteniz
          </span>
        </Reveal>
        <Reveal delay={0.08}>
          <h1 className="max-w-3xl text-4xl font-extrabold tracking-tight sm:text-5xl">
            Sigortanız <span className="text-primary">dakikalar içinde</span>, tamamen dijital
          </h1>
        </Reveal>
        <Reveal delay={0.16}>
          <p className="max-w-2xl text-lg text-muted-foreground">
            Teklif alın, karşılaştırın, satın alın. Poliçeleriniz, hasar bildirimleriniz ve
            yenilemeleriniz tek panelde — evrak yok, telefon trafiği yok.
          </p>
        </Reveal>
        <Reveal delay={0.24}>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <Link to="/register">
              <Button size="lg">Ücretsiz Teklif Al</Button>
            </Link>
            <Link to="/login">
              <Button size="lg" variant="outline">
                Hesabım Var
              </Button>
            </Link>
          </div>
        </Reveal>
        <Reveal delay={0.3}>
          <p className="text-xs text-muted-foreground">
            Kayıt ücretsizdir · Anında fiyat · 3 teminat paketi karşılaştırması
          </p>
        </Reveal>
      </div>
    </section>
  );
}

const SERVICES = [
  { icon: ShieldCheckIcon, image: kaskoImage, title: "Kasko", description: "Aracınız çarpma, çalınma ve doğal afetlere karşı tam güvence altında." },
  { icon: ShieldIcon, image: trafikImage, title: "Trafik Sigortası", description: "Zorunlu trafik sigortanızı dakikalar içinde, en uygun primle yaptırın." },
  { icon: HomeIcon, image: konutImage, title: "Konut Sigortası", description: "Eviniz ve eşyalarınız yangın, hırsızlık ve su baskınına karşı korunsun." },
  { icon: AlertTriangleIcon, image: daskImage, title: "DASK", description: "Zorunlu deprem sigortanızı deprem bölgenize göre doğru fiyatla oluşturun." },
  { icon: HeartIcon, image: saglikImage, title: "Sağlık Sigortası", description: "Yaşınıza ve yaşam tarzınıza uygun sağlık güvencesine hemen kavuşun." },
] as const;

function ServicesSection() {
  return (
    <section className="border-t bg-card/50 py-16 sm:py-20">
      <div className="container space-y-8">
        <Reveal>
          <SectionHeading
            title="Sigorta Hizmetlerimiz"
            subtitle="Beş branşta anında fiyatlama ve dijital poliçe"
          />
        </Reveal>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {SERVICES.map((service, index) => (
            <Reveal key={service.title} delay={index * 0.05}>
              <Link
                to="/register"
                aria-label={`${service.title} için teklif al`}
                className="block h-full rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <Card className="group h-full overflow-hidden hover:-translate-y-1 hover:border-primary/40 hover:shadow-lg">
                  {/* Branşa özel görsel: kartın üst bölümü. Alt kenarda karta doğru koyu gradient blend +
                      okunabilirlik için üstte hafif overlay; sol üstte ikon rozeti. */}
                  <div className="relative aspect-[5/3] overflow-hidden bg-muted">
                    <img
                      src={service.image}
                      alt=""
                      aria-hidden="true"
                      loading="lazy"
                      className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.04]"
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-card via-card/25 to-transparent" />
                    <span className="absolute left-3 top-3 inline-flex h-9 w-9 items-center justify-center rounded-lg bg-background/80 text-primary shadow-sm ring-1 ring-border backdrop-blur transition-colors duration-200 group-hover:bg-primary group-hover:text-primary-foreground">
                      <service.icon className="h-[1.15rem] w-[1.15rem]" />
                    </span>
                  </div>
                  <CardContent className="space-y-2 pt-4">
                    <div className="flex items-center justify-between gap-2">
                      <h3 className="font-semibold">{service.title}</h3>
                      <span
                        aria-hidden="true"
                        className="translate-x-0 text-primary opacity-0 transition-all duration-200 group-hover:translate-x-1 group-hover:opacity-100"
                      >
                        →
                      </span>
                    </div>
                    <p className="text-sm text-muted-foreground">{service.description}</p>
                    <p className="pt-1 text-xs font-medium text-primary">Teklif al</p>
                  </CardContent>
                </Card>
              </Link>
            </Reveal>
          ))}
          <Reveal delay={0.25}>
            <Link
              to="/register"
              className="block h-full rounded-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <Card className="group flex h-full items-center justify-center border-dashed hover:-translate-y-1 hover:border-primary/50 hover:shadow-md">
                <CardContent className="pt-6 text-center">
                  <p className="text-sm text-muted-foreground">
                    Hangi branş size uygun bilmiyor musunuz?
                  </p>
                  <span className="mt-2 inline-block text-sm font-medium text-primary transition-colors group-hover:underline">
                    Teklif sihirbazını deneyin →
                  </span>
                </CardContent>
              </Card>
            </Link>
          </Reveal>
        </div>
      </div>
    </section>
  );
}

const STEPS = [
  { title: "Kayıt olun", description: "Kimlik ve adres bilgilerinizle 2 dakikada hesabınızı oluşturun." },
  { title: "Riskinizi tanımlayın", description: "Aracınızı veya konutunuzu ekleyin; sağlıkta bilgileriniz yeterli." },
  { title: "Paketi seçin", description: "Standart, Genişletilmiş ve Premium paketleri anlık primlerle karşılaştırın." },
  { title: "Güvenceye kavuşun", description: "Kartınızla ödeyin, poliçeniz ve PDF sertifikanız anında hazır." },
] as const;

function HowItWorksSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="container space-y-8">
        <Reveal>
          <SectionHeading title="Nasıl Çalışır?" subtitle="Teklif alımından poliçeye dört basit adım" />
        </Reveal>
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((step, index) => (
            <Reveal key={step.title} delay={index * 0.06}>
              <div className="relative space-y-2 rounded-xl border bg-card p-5">
                <span className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
                  {index + 1}
                </span>
                <h3 className="font-semibold">{step.title}</h3>
                <p className="text-sm text-muted-foreground">{step.description}</p>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}

const ADVANTAGES = [
  { icon: ChartIcon, title: "Şeffaf fiyatlama", description: "Priminizi hangi risk faktörlerinin nasıl etkilediğini kalem kalem görürsünüz." },
  { icon: RefreshIcon, title: "Otomatik yenileme takibi", description: "Poliçenizin bitişine 30 gün kala yenileme teklifiniz otomatik hazırlanır." },
  { icon: FileTextIcon, title: "Anında PDF poliçe", description: "Satın alma tamamlanır tamamlanmaz poliçe sertifikanız indirilmeye hazırdır." },
  { icon: AlertTriangleIcon, title: "Kolay hasar bildirimi", description: "Hasarınızı çevrimiçi bildirin, değerlendirme sürecini adım adım izleyin." },
] as const;

function AdvantagesSection() {
  return (
    <section className="border-t bg-card/50 py-16 sm:py-20">
      <div className="container space-y-8">
        <Reveal>
          <SectionHeading title="Neden SigortaPro?" subtitle="Acenteye gitmeden, beklemeden, kağıtsız" />
        </Reveal>
        <div className="grid gap-6 sm:grid-cols-2">
          {ADVANTAGES.map((advantage, index) => (
            <Reveal key={advantage.title} delay={index * 0.05}>
              <div className="flex gap-4">
                <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent text-accent-foreground">
                  <advantage.icon className="h-5 w-5" />
                </span>
                <div>
                  <h3 className="font-semibold">{advantage.title}</h3>
                  <p className="text-sm text-muted-foreground">{advantage.description}</p>
                </div>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}

const FAQS = [
  {
    question: "Teklif almak ücretli mi?",
    answer:
      "Hayır. Kayıt olduktan sonra dilediğiniz kadar teklif oluşturabilir, üç teminat paketini ücretsiz karşılaştırabilirsiniz. Yalnızca satın almaya karar verdiğinizde ödeme yaparsınız.",
  },
  {
    question: "Primim neye göre hesaplanıyor?",
    answer:
      "Prim; branşa göre sürücü yaşı, araç yaşı, motor gücü, il, bina yaşı, deprem bölgesi gibi risk faktörleriyle hesaplanır. Teklif detayında her faktörün prime etkisini kalem kalem görebilirsiniz.",
  },
  {
    question: "Poliçemi nasıl teslim alırım?",
    answer:
      "Ödemeniz onaylandığı anda poliçeniz oluşur; PDF sertifikanızı Poliçelerim sayfasından anında indirebilirsiniz.",
  },
  {
    question: "Hasar durumunda ne yapmalıyım?",
    answer:
      "Portaldaki Hasarlarım bölümünden olayınızı bildirmeniz yeterli. Bildiriminiz uzmanlarımızca incelenir; onay ve ödeme adımlarını zaman çizelgesinden takip edersiniz.",
  },
  {
    question: "Poliçem biterken ne olur?",
    answer:
      "Bitişe 30 gün kala güncel bilgilerinizle bir yenileme teklifi otomatik hazırlanır. Tek tıkla onaylayıp ödeyerek kesintisiz güvencenizi sürdürürsünüz.",
  },
] as const;

function FaqSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="container max-w-3xl space-y-8">
        <Reveal>
          <SectionHeading title="Sık Sorulan Sorular" subtitle="Merak ettikleriniz" />
        </Reveal>
        <Reveal>
          <div className="divide-y rounded-xl border bg-card">
            {FAQS.map((faq) => (
              <details key={faq.question} className="group px-5 py-4">
                <summary className="flex cursor-pointer list-none items-center justify-between gap-4 font-medium [&::-webkit-details-marker]:hidden">
                  {faq.question}
                  <span className="text-muted-foreground transition-transform group-open:rotate-45">+</span>
                </summary>
                <p className="pt-2 text-sm text-muted-foreground">{faq.answer}</p>
              </details>
            ))}
          </div>
        </Reveal>
      </div>
    </section>
  );
}

function CtaSection() {
  return (
    <section className="container pb-16 sm:pb-20">
      <Reveal>
        <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary via-primary/80 to-slate-900 px-6 py-12 text-center text-white sm:px-12">
          <div className="pointer-events-none absolute -left-10 -top-16 h-40 w-40 rounded-full bg-white/15 blur-2xl" />
          <h2 className="text-2xl font-bold sm:text-3xl">Güvenceniz birkaç dakika uzağınızda</h2>
          <p className="mx-auto mt-2 max-w-xl text-white/80">
            Hemen kayıt olun, ilk teklifinizi oluşturun ve priminizi anında görün.
          </p>
          <Link to="/register" className="mt-6 inline-block">
            <Button size="lg" variant="secondary">
              Ücretsiz Hesap Oluştur
            </Button>
          </Link>
        </div>
      </Reveal>
    </section>
  );
}

function LandingFooter() {
  return (
    <footer className="border-t py-8">
      <div className="container flex flex-col items-center justify-between gap-4 text-sm text-muted-foreground sm:flex-row">
        <span className="flex items-center gap-2 font-semibold text-foreground">
          <ShieldIcon className="h-4 w-4 text-primary" />
          SigortaPro
        </span>
        <nav className="flex gap-4">
          <Link to="/login" className="hover:text-foreground">Giriş Yap</Link>
          <Link to="/register" className="hover:text-foreground">Kayıt Ol</Link>
          <Link to="/forgot-password" className="hover:text-foreground">Şifremi Unuttum</Link>
        </nav>
        <span>SigortaPro — MVP demo. Gerçek bir sigorta ürünü satmaz.</span>
      </div>
    </footer>
  );
}

function SectionHeading({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div className="text-center">
      <h2 className="text-3xl font-bold tracking-tight">{title}</h2>
      <p className="mt-2 text-muted-foreground">{subtitle}</p>
    </div>
  );
}

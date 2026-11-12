import {
  ArrowDownRight,
  ArrowRight,
  ArrowUpRight,
  BadgeCheck,
  ChartNoAxesCombined,
  CircleDollarSign,
  CircleHelp,
  Clock3,
  CreditCard,
  Gauge,
  Lightbulb,
  LockKeyhole,
  RotateCcw,
  ShieldCheck,
  Sparkles,
  Waypoints,
} from 'lucide-react'
import { Link } from 'react-router'
import { LanguageMenu } from '@/components/layout/LanguageMenu'
import { Logo } from '@/components/layout/Logo'
import { Button } from '@/components/ui'
import { useI18n } from '@/i18n'

const chartBars = [29, 42, 34, 50, 45, 59, 51, 68, 61, 76, 67, 82, 74, 94]

export function HomePage() {
  const { t } = useI18n()

  const navigation = [
    { href: '#platform', label: t('landing.navPlatform') },
    { href: '#how-it-works', label: t('landing.navProcess') },
    { href: '#about', label: t('landing.navAbout') },
  ]

  const features = [
    {
      icon: ChartNoAxesCombined,
      title: t('landing.featureRevenueTitle'),
      description: t('landing.featureRevenueDescription'),
      tone: 'bg-brand-soft text-brand-text',
    },
    {
      icon: RotateCcw,
      title: t('landing.featureRecoveryTitle'),
      description: t('landing.featureRecoveryDescription'),
      tone: 'bg-success-soft text-success',
    },
    {
      icon: Gauge,
      title: t('landing.featureAiTitle'),
      description: t('landing.featureAiDescription'),
      tone: 'bg-info-soft text-info',
    },
  ]

  const principles = [
    { icon: Lightbulb, title: t('landing.principleClarityTitle'), description: t('landing.principleClarityDescription') },
    { icon: LockKeyhole, title: t('landing.principleControlTitle'), description: t('landing.principleControlDescription') },
    { icon: CircleDollarSign, title: t('landing.principleCostTitle'), description: t('landing.principleCostDescription') },
  ]

  return (
    <div className="min-h-dvh overflow-x-clip bg-surface-app text-ink">
      <header className="sticky top-0 z-30 border-b border-line/80 bg-surface-app/90 backdrop-blur-xl">
        <div className="mx-auto flex h-[4.5rem] max-w-7xl items-center gap-3 px-4 sm:px-8 lg:px-12">
          <div className="flex shrink-0 items-center gap-3">
            <Link to="/" aria-label={t('landing.home')} className="rounded-lg focus-ring">
              <Logo />
            </Link>
            <a
              href="#top"
              aria-current="page"
              className="hidden items-center rounded-full border border-line bg-surface-card px-3.5 py-2 text-sm font-semibold text-ink-soft transition-colors hover:border-brand-300 hover:text-ink focus-ring sm:inline-flex"
            >
              {t('landing.home')}
            </a>
          </div>

          <nav aria-label={t('landing.sectionNavigation')} className="ml-auto hidden items-center gap-1 lg:flex">
            {navigation.map(({ href, label }) => (
              <a
                key={href}
                href={href}
                className="rounded-full px-3.5 py-2 text-sm font-semibold text-ink-soft transition-colors hover:bg-surface-muted hover:text-ink focus-ring"
              >
                {label}
              </a>
            ))}
          </nav>

          <div className="ml-auto flex shrink-0 items-center gap-2 lg:ml-3">
            <LanguageMenu />
            <Link to="/login" className="rounded-lg focus-ring">
              <Button size="sm">
                {t('landing.login')}
                <ArrowRight aria-hidden="true" size={16} />
              </Button>
            </Link>
          </div>
        </div>
      </header>

      <main id="top">
        <section className="relative isolate overflow-hidden bg-[#10141b] text-white">
          <div aria-hidden="true" className="hero-grid pointer-events-none absolute inset-0 opacity-80" />
          <div className="pointer-events-none absolute -left-40 top-8 size-[30rem] rounded-full bg-brand-500/20 blur-[120px]" />
          <div className="pointer-events-none absolute -right-32 bottom-0 size-[28rem] rounded-full bg-sky-500/10 blur-[120px]" />
          <div aria-hidden="true" className="pointer-events-none absolute right-[12%] top-20 hidden size-64 rounded-full border border-brand-200/10 motion-safe:animate-float lg:block" />
          <div aria-hidden="true" className="pointer-events-none absolute right-[14%] top-28 hidden size-52 rounded-full border border-dashed border-white/10 motion-safe:animate-orbit lg:block" />
          <div aria-hidden="true" className="pointer-events-none absolute bottom-12 left-[7%] hidden size-48 motion-safe:animate-float lg:block">
            <div className="absolute inset-0 rounded-full border border-sky-200/10 motion-safe:animate-orbit" />
            <div className="absolute inset-5 rounded-full border border-dashed border-brand-200/10 motion-safe:animate-orbit-reverse" />
          </div>
          <div className="relative mx-auto grid max-w-7xl items-center gap-14 px-5 py-16 sm:px-8 sm:py-20 lg:grid-cols-[0.92fr_1.08fr] lg:gap-10 lg:px-12 lg:py-28">
            <div className="animate-rise-in">
              <div className="mb-7 inline-flex items-center gap-3 rounded-full border border-brand-300/30 bg-brand-400/10 px-5 py-3 text-sm font-bold tracking-wide text-brand-100 sm:text-base">
                <Sparkles aria-hidden="true" size={19} className="text-brand-300" />
                {t('landing.eyebrow')}
              </div>
              <h1 className="max-w-2xl text-4xl font-extrabold leading-[1.08] tracking-[-0.045em] sm:text-5xl lg:text-6xl xl:text-7xl">
                {t('landing.headline')}
                <span className="mt-2 block bg-gradient-to-r from-brand-300 via-orange-200 to-amber-100 bg-clip-text text-transparent">
                  {t('landing.headlineAccent')}
                </span>
              </h1>
              <p className="mt-7 max-w-xl text-base leading-7 text-slate-300 sm:text-lg sm:leading-8">
                {t('landing.description')}
              </p>
              <div className="mt-9 flex flex-wrap items-center gap-4">
                <Link to="/login" className="rounded-lg focus-ring">
                  <Button size="lg">
                    {t('landing.primaryCta')}
                    <ArrowRight aria-hidden="true" size={18} />
                  </Button>
                </Link>
                <a
                  href="#how-it-works"
                  className="inline-flex min-h-12 items-center gap-2 rounded-lg border border-white/15 px-5 text-sm font-bold text-white transition-colors hover:border-white/35 hover:bg-white/5 focus-ring"
                >
                  {t('landing.secondaryCta')}
                  <ArrowDownRight aria-hidden="true" size={17} />
                </a>
              </div>
              <div className="mt-10 flex flex-wrap gap-3">
                {[t('landing.heroPillPayments'), t('landing.heroPillRecovery'), t('landing.heroPillCosts')].map((label) => (
                  <span key={label} className="inline-flex items-center gap-2 rounded-xl border border-white/10 bg-white/[0.06] px-4 py-3 text-sm font-semibold text-slate-200">
                    <BadgeCheck aria-hidden="true" size={17} className="text-brand-300" />
                    {label}
                  </span>
                ))}
              </div>
            </div>

            <div className="relative mx-auto w-full max-w-[42rem] animate-rise-in [animation-delay:120ms] lg:rotate-[0.8deg] lg:transition-transform lg:duration-700 lg:hover:rotate-0">
              <div className="absolute -inset-8 rounded-[3rem] bg-gradient-to-br from-brand-500/20 via-transparent to-sky-400/15 blur-2xl" />
              <div className="relative overflow-hidden rounded-[1.75rem] border border-white/15 bg-[#f7f8fa] p-3 text-slate-900 shadow-2xl shadow-black/30 sm:rounded-[2rem] sm:p-5">
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-2 pb-4">
                  <div className="flex items-center gap-3">
                    <span className="grid size-10 place-items-center rounded-xl bg-[#10141b] text-brand-300">
                      <ChartNoAxesCombined aria-hidden="true" size={20} />
                    </span>
                    <div>
                      <p className="font-bold">{t('landing.previewTitle')}</p>
                      <p className="mt-0.5 text-xs text-slate-500">{t('landing.previewSubtitle')}</p>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3 py-4">
                  <div className="rounded-2xl border border-slate-200 bg-white p-4 sm:p-5">
                    <div className="flex items-center justify-between gap-2 text-sm font-semibold text-slate-500">
                      {t('landing.previewPayments')}
                      <CreditCard aria-hidden="true" size={18} className="text-brand-600" />
                    </div>
                    <div className="mt-5 flex items-end justify-between gap-2">
                      <span className="text-xl font-extrabold tracking-tight sm:text-2xl">{t('landing.previewStatus')}</span>
                      <span className="inline-flex items-center gap-1 text-xs font-bold text-emerald-700">
                        <ArrowUpRight aria-hidden="true" size={14} />
                        {t('landing.previewSynced')}
                      </span>
                    </div>
                  </div>
                  <div className="rounded-2xl border border-slate-200 bg-white p-4 sm:p-5">
                    <div className="flex items-center justify-between gap-2 text-sm font-semibold text-slate-500">
                      {t('landing.previewAiCosts')}
                      <Gauge aria-hidden="true" size={18} className="text-sky-700" />
                    </div>
                    <div className="mt-5 flex items-end justify-between gap-2">
                      <span className="text-xl font-extrabold tracking-tight sm:text-2xl">{t('landing.previewVisible')}</span>
                      <span className="inline-flex items-center gap-1 text-xs font-bold text-sky-700">
                        <Sparkles aria-hidden="true" size={14} />
                        {t('landing.previewTracked')}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-white p-4 sm:p-5">
                  <div className="mb-5 flex items-center justify-between gap-4">
                    <div>
                      <p className="text-sm font-bold">{t('landing.chartTitle')}</p>
                    </div>
                    <span className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1.5 text-xs font-bold text-slate-600">
                      <Clock3 aria-hidden="true" size={14} />
                      {t('landing.previewRange')}
                    </span>
                  </div>
                  <div className="relative flex h-32 items-end gap-1.5 sm:h-40 sm:gap-2">
                    <div className="absolute inset-0 flex flex-col justify-between" aria-hidden="true">
                      <span className="border-t border-dashed border-slate-200" />
                      <span className="border-t border-dashed border-slate-200" />
                      <span className="border-t border-dashed border-slate-200" />
                    </div>
                    {chartBars.map((height, index) => (
                      <span
                        key={index}
                        aria-hidden="true"
                        className={`relative z-10 min-w-0 flex-1 rounded-t-md ${index === chartBars.length - 1 ? 'bg-brand-500' : 'bg-brand-200'}`}
                        style={{ height: `${height}%`, animation: `rise-in 500ms var(--ease-out-soft) ${index * 35}ms both` }}
                      />
                    ))}
                  </div>
                  <div className="mt-3 flex justify-between text-[11px] font-medium text-slate-400">
                    <span>{t('landing.chartStart')}</span>
                    <span>{t('landing.chartEnd')}</span>
                  </div>
                </div>

                <div className="mt-3 flex items-center gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 p-4">
                  <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-white text-emerald-700 shadow-sm">
                    <Lightbulb aria-hidden="true" size={19} />
                  </span>
                  <div>
                    <p className="text-sm font-bold">{t('landing.previewInsightTitle')}</p>
                    <p className="mt-1 text-xs leading-5 text-slate-600">{t('landing.previewInsightDescription')}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section id="platform" className="scroll-mt-24 border-b border-line bg-surface-card">
          <div className="mx-auto max-w-7xl px-5 py-20 sm:px-8 sm:py-24 lg:px-12">
            <div className="mx-auto max-w-3xl text-center">
              <p className="text-sm font-extrabold uppercase tracking-[0.18em] text-brand-text">{t('landing.featuresEyebrow')}</p>
              <h2 className="mt-4 text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl">{t('landing.featuresTitle')}</h2>
              <p className="mx-auto mt-5 max-w-2xl text-base leading-7 text-ink-soft sm:text-lg">{t('landing.featuresDescription')}</p>
            </div>
            <div className="mt-12 grid gap-5 md:grid-cols-3">
              {features.map(({ icon: Icon, title, description, tone }, index) => (
                <article
                  key={title}
                  className="group rounded-3xl border border-line bg-surface-app p-6 transition-all duration-300 hover:-translate-y-1 hover:border-brand-300 hover:shadow-lg motion-safe:animate-rise-in sm:p-8"
                  style={{ animationDelay: `${index * 90}ms` }}
                >
                  <span className={`grid size-14 place-items-center rounded-2xl ${tone} transition-transform duration-300 group-hover:scale-105`}>
                    <Icon aria-hidden="true" size={25} />
                  </span>
                  <h3 className="mt-7 text-xl font-extrabold tracking-tight">{title}</h3>
                  <p className="mt-3 text-sm leading-7 text-ink-soft sm:text-base">{description}</p>
                  <a href="#how-it-works" className="mt-6 inline-flex items-center gap-2 text-sm font-bold text-brand-text focus-ring">
                    {t('landing.learnMore')}
                    <ArrowRight aria-hidden="true" size={16} className="transition-transform group-hover:translate-x-1" />
                  </a>
                </article>
              ))}
            </div>

            <div className="mt-8 grid gap-5 rounded-3xl bg-[#161b23] p-6 text-white sm:p-9 lg:grid-cols-[1fr_auto] lg:items-center lg:px-12">
              <div>
                <p className="text-sm font-bold uppercase tracking-[0.16em] text-brand-300">{t('landing.platformNoteEyebrow')}</p>
                <h3 className="mt-3 max-w-2xl text-2xl font-extrabold tracking-tight sm:text-3xl">{t('landing.platformNoteTitle')}</h3>
                <p className="mt-3 max-w-2xl text-sm leading-7 text-slate-300 sm:text-base">{t('landing.platformNoteDescription')}</p>
              </div>
              <div className="flex flex-wrap gap-3 lg:max-w-xs lg:justify-end">
                {[t('landing.platformTagPayments'), t('landing.platformTagRecovery'), t('landing.platformTagAi')].map((label) => (
                  <span key={label} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-3 text-sm font-bold text-slate-100">
                    {label}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </section>

        <section id="how-it-works" className="scroll-mt-24">
          <div className="mx-auto max-w-7xl px-5 py-20 sm:px-8 sm:py-24 lg:px-12">
            <div className="grid gap-8 lg:grid-cols-[0.8fr_1.2fr] lg:items-end">
              <div>
                <p className="text-sm font-extrabold uppercase tracking-[0.18em] text-brand-text">{t('landing.processEyebrow')}</p>
                <h2 className="mt-4 text-3xl font-extrabold tracking-tight sm:text-4xl">{t('landing.processTitle')}</h2>
              </div>
              <p className="max-w-2xl text-base leading-7 text-ink-soft sm:text-lg">{t('landing.processDescription')}</p>
            </div>

            <div className="mt-12 grid gap-4 lg:grid-cols-3">
              {[
                { icon: CreditCard, number: '01', title: t('landing.stepOneTitle'), description: t('landing.stepOneDescription') },
                { icon: Waypoints, number: '02', title: t('landing.stepTwoTitle'), description: t('landing.stepTwoDescription') },
                { icon: ShieldCheck, number: '03', title: t('landing.stepThreeTitle'), description: t('landing.stepThreeDescription') },
              ].map(({ icon: Icon, number, title, description }, index) => (
                <article
                  key={number}
                  className="relative overflow-hidden rounded-3xl border border-line bg-surface-card p-6 shadow-xs transition-[transform,box-shadow,border-color] duration-300 hover:-translate-y-1 hover:border-brand-300 hover:shadow-md motion-safe:animate-rise-in sm:p-8"
                  style={{ animationDelay: `${index * 90}ms` }}
                >
                  <div className="flex items-center justify-between">
                    <span className="grid size-12 place-items-center rounded-2xl bg-brand-soft text-brand-text">
                      <Icon aria-hidden="true" size={22} />
                    </span>
                    <span className="font-mono text-4xl font-bold tracking-tight text-brand-200">{number}</span>
                  </div>
                  <h3 className="mt-7 text-xl font-extrabold">{title}</h3>
                  <p className="mt-3 text-sm leading-7 text-ink-soft sm:text-base">{description}</p>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section id="about" className="scroll-mt-24 border-y border-line bg-surface-card">
          <div className="mx-auto grid max-w-7xl gap-12 px-5 py-20 sm:px-8 sm:py-24 lg:grid-cols-[0.95fr_1.05fr] lg:items-center lg:px-12">
            <div>
              <p className="text-sm font-extrabold uppercase tracking-[0.18em] text-brand-text">{t('landing.aboutEyebrow')}</p>
              <h2 className="mt-4 text-3xl font-extrabold leading-tight tracking-tight sm:text-4xl lg:text-5xl">{t('landing.aboutTitle')}</h2>
              <p className="mt-6 text-base leading-8 text-ink-soft">{t('landing.aboutDescription')}</p>
              <div className="mt-8 flex items-start gap-4 rounded-2xl border border-brand-200 bg-brand-50 p-5 dark:border-brand-800 dark:bg-brand-900/20">
                <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-white text-brand-700 shadow-xs dark:bg-brand-950 dark:text-brand-200">
                  <CircleHelp aria-hidden="true" size={21} />
                </span>
                <p className="text-sm font-semibold leading-6 text-ink">{t('landing.aboutPromise')}</p>
              </div>
            </div>
            <div className="grid gap-4">
              {principles.map(({ icon: Icon, title, description }) => (
                <article
                  key={title}
                  className="flex gap-4 rounded-2xl border border-line bg-surface-app p-5 transition-[transform,box-shadow,border-color] duration-300 hover:-translate-y-0.5 hover:border-brand-300 hover:shadow-sm sm:p-6"
                >
                  <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-surface-card text-brand-text shadow-xs">
                    <Icon aria-hidden="true" size={22} />
                  </span>
                  <div>
                    <h3 className="font-extrabold">{title}</h3>
                    <p className="mt-2 text-sm leading-6 text-ink-soft">{description}</p>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="relative isolate overflow-hidden bg-[#10141b] text-white">
          <div aria-hidden="true" className="hero-grid pointer-events-none absolute inset-0 opacity-80" />
          <div className="pointer-events-none absolute -right-24 -top-32 size-96 rounded-full bg-brand-500/20 blur-[100px]" />
          <div className="relative mx-auto flex max-w-7xl flex-col items-start justify-between gap-8 px-5 py-16 sm:px-8 sm:py-20 md:flex-row md:items-center lg:px-12">
            <div>
              <p className="text-sm font-extrabold uppercase tracking-[0.18em] text-brand-300">{t('landing.closingEyebrow')}</p>
              <h2 className="mt-3 max-w-3xl text-3xl font-extrabold tracking-tight sm:text-4xl">{t('landing.closingTitle')}</h2>
              <p className="mt-4 max-w-2xl text-base leading-7 text-slate-300">{t('landing.closingDescription')}</p>
            </div>
            <Link to="/login" className="shrink-0 rounded-lg focus-ring">
              <Button size="lg">
                {t('landing.primaryCta')}
                <ArrowRight aria-hidden="true" size={18} />
              </Button>
            </Link>
          </div>
        </section>
      </main>

      <footer className="border-t border-line px-5 py-8 sm:px-8 lg:px-12">
        <div className="mx-auto flex max-w-7xl flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <Link to="/" aria-label={t('landing.home')} className="rounded-lg focus-ring">
            <Logo />
          </Link>
          <nav aria-label={t('landing.sectionNavigation')} className="flex flex-wrap gap-x-5 gap-y-2 text-sm font-semibold text-ink-soft">
            {navigation.map(({ href, label }) => (
              <a key={href} href={href} className="hover:text-ink focus-ring">
                {label}
              </a>
            ))}
          </nav>
          <p className="text-xs leading-5 text-ink-faint">{t('landing.footer')}</p>
        </div>
      </footer>
    </div>
  )
}

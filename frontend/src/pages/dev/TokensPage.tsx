import type { ReactNode } from 'react'
import { env } from '@/config/env'
import { useFormat } from '@/i18n/useFormat'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'
import { DevHeader } from './DevHeader'

const brandScale = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const
const brandClass: Record<(typeof brandScale)[number], string> = {
  50: 'bg-brand-50',
  100: 'bg-brand-100',
  200: 'bg-brand-200',
  300: 'bg-brand-300',
  400: 'bg-brand-400',
  500: 'bg-brand-500',
  600: 'bg-brand-600',
  700: 'bg-brand-700',
  800: 'bg-brand-800',
  900: 'bg-brand-900',
}

// Token names are code identifiers and are shown as-is on purpose.
const surfaces = [
  { name: 'surface-app', cls: 'bg-surface-app' },
  { name: 'surface-card', cls: 'bg-surface-card' },
  { name: 'surface-muted', cls: 'bg-surface-muted' },
]
const status = [
  { name: 'success', cls: 'bg-success-soft text-success' },
  { name: 'warning', cls: 'bg-warning-soft text-warning' },
  { name: 'danger', cls: 'bg-danger-soft text-danger' },
  { name: 'info', cls: 'bg-info-soft text-info' },
]
const radii = [
  { name: 'sm', cls: 'rounded-sm' },
  { name: 'md', cls: 'rounded-md' },
  { name: 'lg', cls: 'rounded-lg' },
  { name: 'xl', cls: 'rounded-xl' },
  { name: '2xl', cls: 'rounded-2xl' },
]
const shadows = [
  { name: 'xs', cls: 'shadow-xs' },
  { name: 'sm', cls: 'shadow-sm' },
  { name: 'md', cls: 'shadow-md' },
  { name: 'lg', cls: 'shadow-lg' },
]

// Code sample for the monospace font. Intentionally not translated.
const MONO_SAMPLE = 'tok_in 1,204 · tok_out 386 · 0.0042 USD'

function Section({ title, hint, children }: { title: string; hint?: string; children: ReactNode }) {
  return (
    <section className="animate-rise-in rounded-xl border border-line bg-surface-card p-6 shadow-sm">
      <header className="mb-5">
        <h2 className="text-sm font-semibold tracking-tight">{title}</h2>
        {hint && <p className="mt-1 text-xs text-ink-soft">{hint}</p>}
      </header>
      {children}
    </section>
  )
}

export function TokensPage() {
  const { t } = useI18n()
  const f = useFormat()

  return (
    <div className="min-h-dvh bg-surface-app">
      <DevHeader current="tokens" />

      <main className="mx-auto grid max-w-6xl grid-cols-[minmax(0,1fr)] gap-6 px-4 py-10 sm:px-6">
        <div className="animate-fade-in">
          <p className="tabular text-xs font-medium uppercase tracking-widest text-brand-text">{t('dev.tokens.eyebrow')}</p>
          <h1 className="mt-2 max-w-2xl text-3xl font-bold tracking-tight sm:text-4xl">{t('dev.tokens.title')}</h1>
          <p className="mt-3 max-w-xl text-ink-soft">{t('dev.tokens.intro')}</p>
          <div className="mt-4 inline-flex items-center gap-2 rounded-full border border-line bg-surface-card px-3 py-1 text-xs text-ink-soft">
            <span className="size-1.5 rounded-full bg-brand-500" />
            {t('dev.tokens.dataSource')}: <span className="tabular text-ink">{env.dataSource}</span>
          </div>
        </div>

        <Section title={t('dev.tokens.brandTitle')} hint={t('dev.tokens.brandHint')}>
          <div className="grid grid-cols-5 gap-2 sm:grid-cols-10">
            {brandScale.map((step) => (
              <div key={step} className="grid gap-1.5">
                <div className={cn('h-14 rounded-md border border-line', brandClass[step])} />
                <span className="tabular text-center text-[11px] text-ink-soft">{step}</span>
              </div>
            ))}
          </div>
        </Section>

        <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <Section title={t('dev.tokens.surfacesTitle')} hint={t('dev.tokens.surfacesHint')}>
            <div className="grid grid-cols-3 gap-3">
              {surfaces.map((s) => (
                <div key={s.name} className="grid gap-1.5">
                  <div className={cn('h-16 rounded-md border border-line-strong', s.cls)} />
                  <span className="tabular text-[11px] text-ink-soft">{s.name}</span>
                </div>
              ))}
            </div>
          </Section>

          <Section title={t('dev.tokens.statusTitle')} hint={t('dev.tokens.statusHint')}>
            <div className="flex flex-wrap gap-2">
              {status.map((s) => (
                <span key={s.name} className={cn('rounded-full px-3 py-1 text-xs font-medium', s.cls)}>
                  {s.name}
                </span>
              ))}
            </div>
          </Section>
        </div>

        <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <Section title={t('dev.tokens.radiusTitle')}>
            <div className="flex flex-wrap items-end gap-3">
              {radii.map((r) => (
                <div key={r.name} className="grid gap-1.5 text-center">
                  <div className={cn('size-14 border border-line-strong bg-surface-muted', r.cls)} />
                  <span className="tabular text-[11px] text-ink-soft">{r.name}</span>
                </div>
              ))}
            </div>
          </Section>

          <Section title={t('dev.tokens.shadowsTitle')}>
            <div className="flex flex-wrap items-end gap-4 rounded-lg bg-surface-app p-4">
              {shadows.map((s) => (
                <div key={s.name} className="grid gap-2 text-center">
                  <div className={cn('size-14 rounded-lg bg-surface-raised', s.cls)} />
                  <span className="tabular text-[11px] text-ink-soft">{s.name}</span>
                </div>
              ))}
            </div>
          </Section>
        </div>

        <Section title={t('dev.tokens.typographyTitle')} hint={t('dev.tokens.typographyHint')}>
          <div className="grid gap-4">
            <p className="text-4xl font-bold tracking-tight">{t('dev.tokens.sampleDisplay')}</p>
            <p className="text-xl font-semibold">{t('dev.tokens.sampleHeading')}</p>
            <p className="text-base text-ink-soft">{t('dev.tokens.sampleBody')}</p>
            <p className="tabular text-3xl font-medium">{f.money({ amount: '128430.52', currency: 'USD' })}</p>
            <p className="tabular text-sm text-ink-faint">{MONO_SAMPLE}</p>
          </div>
        </Section>

        <Section title={t('dev.tokens.brandSurfacesTitle')}>
          <div className="flex flex-wrap items-center gap-4">
            <div className="rounded-lg bg-brand-gradient px-5 py-3 text-sm font-semibold text-ink-on-brand shadow-brand">
              {t('dev.tokens.gradient')}
            </div>
            <div className="rounded-lg bg-brand-soft px-5 py-3 text-sm font-semibold text-brand-text">
              {t('dev.tokens.softAccent')}
            </div>
            <div className="bg-surface-gradient rounded-lg border border-line px-5 py-3 text-sm font-semibold">
              {t('dev.tokens.cardGradient')}
            </div>
          </div>
        </Section>
      </main>
    </div>
  )
}

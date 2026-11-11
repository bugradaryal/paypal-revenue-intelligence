import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { Button, Field, Input, ThemeToggle } from '@/components/ui'
import { LanguageMenu } from '@/components/layout/LanguageMenu'
import { LogoMark } from '@/components/layout/Logo'
import { useAuth } from '@/features/auth/AuthProvider'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { getLogoutReason } from '@/services/auth'

export function LoginPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { required, user, signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/dashboard'

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!required || user) return <Navigate to={from} replace />

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      await signIn(email.trim(), password)
      navigate(from, { replace: true })
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="min-h-dvh bg-surface-app lg:grid lg:grid-cols-[minmax(0,1.08fr)_minmax(420px,0.92fr)]">
      <aside className="relative isolate flex min-h-[220px] overflow-hidden bg-[#10141b] px-6 py-6 text-white sm:min-h-[280px] sm:px-10 lg:min-h-dvh lg:px-12 lg:py-10">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 bg-[radial-gradient(ellipse_at_24%_22%,rgba(242,140,69,0.18),transparent_40%),radial-gradient(ellipse_at_82%_72%,rgba(16,185,129,0.12),transparent_42%)]" />
        <img
          src="/login-illustration.svg"
          alt=""
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 -z-10 size-full object-cover object-center opacity-90"
        />
        <Link to="/" aria-label={t('landing.home')} className="relative z-10 inline-flex h-fit items-center gap-3 rounded-lg focus-ring">
          <LogoMark className="size-10" />
          <span className="grid gap-0.5 leading-tight">
            <span className="text-sm font-bold tracking-tight">{t('app.name')}</span>
            <span className="text-[11px] font-medium text-slate-300">{t('app.vendor')}</span>
          </span>
        </Link>
      </aside>

      <section className="relative flex min-h-[calc(100dvh-220px)] flex-col sm:min-h-[calc(100dvh-280px)] lg:min-h-dvh">
        <header className="flex items-center justify-end gap-2 px-4 py-4 sm:px-8 sm:py-6">
          <LanguageMenu />
          <ThemeToggle />
        </header>

        <main className="grid flex-1 place-items-center px-6 pb-12 sm:px-10 lg:px-12">
          <div className="w-full max-w-[400px]">
            <div className="mb-8 grid size-12 place-items-center rounded-2xl border border-brand-200/70 bg-brand-soft shadow-sm">
              <LogoMark className="size-9" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight sm:text-[1.75rem]">{t('auth.title')}</h1>
            <p className="mt-2 text-sm leading-6 text-ink-soft">{t('auth.description')}</p>

            {getLogoutReason() === 'expired' && !error && (
              <p role="status" className="mt-5 rounded-lg border border-line bg-surface-muted px-4 py-3 text-sm text-ink-soft">
                {t('auth.sessionExpired')}
              </p>
            )}

            <form onSubmit={onSubmit} className="mt-8 grid gap-5" noValidate>
              <Field label={t('auth.email')} required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="email"
                    autoComplete="username"
                    required
                    maxLength={254}
                    value={email}
                    invalid={invalid}
                    aria-describedby={describedBy}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                )}
              </Field>
              <Field label={t('auth.password')} required error={error ?? undefined}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="password"
                    autoComplete="current-password"
                    required
                    maxLength={128}
                    value={password}
                    invalid={invalid}
                    aria-describedby={describedBy}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                )}
              </Field>
              <Button type="submit" className="mt-1 w-full" loading={pending} disabled={!email || !password}>
                {t('auth.submit')}
              </Button>
            </form>
          </div>
        </main>
      </section>
    </div>
  )
}

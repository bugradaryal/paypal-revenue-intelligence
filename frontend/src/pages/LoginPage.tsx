import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router'
import { Button, Field, Input, ThemeToggle } from '@/components/ui'
import { LanguageMenu } from '@/components/layout/LanguageMenu'
import { LogoMark } from '@/components/layout/Logo'
import { useAuth } from '@/features/auth/AuthProvider'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { getLogoutReason, register } from '@/services/auth'
import { ApiError } from '@/services/errors'

export function LoginPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { required, user, signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? '/dashboard'

  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [notice, setNotice] = useState<'submitted' | 'pendingApproval' | null>(null)

  if (!required || user) return <Navigate to={from} replace />

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      if (mode === 'register') {
        await register({ email: email.trim(), displayName: displayName.trim(), password })
        setMode('login')
        setNotice('submitted')
        setPassword('')
        return
      }

      await signIn(email.trim(), password)
      navigate(from, { replace: true })
    } catch (e) {
      if (e instanceof ApiError && e.code === 'account_pending_approval') {
        setNotice('pendingApproval')
        setPassword('')
        return
      }
      setError(errorMessage(e))
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="min-h-dvh bg-surface-app lg:grid lg:grid-cols-[minmax(0,1.08fr)_minmax(420px,0.92fr)]">
      <aside className="relative isolate flex min-h-[320px] flex-col overflow-hidden bg-[#10141b] px-6 py-6 text-white sm:min-h-[380px] sm:px-10 lg:min-h-dvh lg:px-12 lg:py-10">
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 z-0 bg-[radial-gradient(ellipse_at_24%_22%,rgba(242,140,69,0.22),transparent_40%),radial-gradient(ellipse_at_82%_72%,rgba(16,185,129,0.14),transparent_42%)]" />
        <img
          src="/login-illustration.svg"
          alt=""
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 z-0 size-full object-cover object-center opacity-45"
        />
        <div aria-hidden="true" className="pointer-events-none absolute inset-0 z-0 overflow-hidden">
          <div className="absolute inset-[18%] rounded-full border border-white/10 motion-safe:animate-orbit">
            <span className="absolute left-[74%] top-[8%] size-2 rounded-full bg-brand-300 shadow-[0_0_18px_rgba(242,140,69,0.9)]" />
          </div>
          <div className="absolute inset-[27%] rounded-full border border-emerald-200/10 motion-safe:animate-orbit-reverse">
            <span className="absolute bottom-[13%] left-[17%] size-1.5 rounded-full bg-emerald-200 shadow-[0_0_16px_rgba(110,231,183,0.8)]" />
          </div>
          <span className="absolute left-[18%] top-[34%] size-2 rounded-full bg-brand-200/80 motion-safe:animate-float" />
          <span className="absolute bottom-[19%] right-[22%] size-1.5 rounded-full bg-emerald-200/80 motion-safe:animate-float" />
          <div className="absolute inset-0 bg-gradient-to-br from-[#10141b]/60 via-transparent to-[#10141b]/75" />
        </div>
        <Link to="/" aria-label={t('landing.home')} className="relative z-10 inline-flex h-fit items-center gap-3 rounded-lg focus-ring">
          <LogoMark className="size-10" />
          <span className="grid gap-0.5 leading-tight">
            <span className="text-sm font-bold tracking-tight">{t('app.name')}</span>
            <span className="text-[11px] font-medium text-slate-300">{t('app.vendor')}</span>
          </span>
        </Link>
        <div className="relative z-10 mx-auto flex w-full max-w-2xl flex-1 flex-col items-center justify-center py-8 text-center">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-brand-200">{t('app.vendor')}</p>
          <h2 className="mt-4 max-w-xl text-2xl font-bold tracking-tight sm:text-3xl lg:text-4xl">
            {t('auth.productInfoTitle')}
          </h2>
          <p className="mt-4 max-w-lg text-sm leading-6 text-slate-300 sm:text-base">
            {t('auth.productInfoDescription')}
          </p>
          <ul className="mt-8 hidden w-full max-w-xl gap-3 text-left sm:grid sm:grid-cols-3">
            {[t('auth.productFeaturePayments'), t('auth.productFeatureRecovery'), t('auth.productFeatureAi')].map((feature) => (
              <li key={feature} className="rounded-xl border border-white/10 bg-white/[0.06] px-4 py-3 text-xs leading-5 text-slate-200 shadow-lg backdrop-blur-md">
                <span aria-hidden="true" className="mb-2 block size-1.5 rounded-full bg-brand-300" />
                {feature}
              </li>
            ))}
          </ul>
        </div>
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
            <h1 className="text-2xl font-bold tracking-tight sm:text-[1.75rem]">
              {mode === 'login' ? t('auth.title') : t('auth.registerTitle')}
            </h1>
            <p className="mt-2 text-sm leading-6 text-ink-soft">
              {mode === 'login' ? t('auth.description') : t('auth.registerDescription')}
            </p>

            {getLogoutReason() === 'expired' && !error && (
              <p role="status" className="mt-5 rounded-lg border border-line bg-surface-muted px-4 py-3 text-sm text-ink-soft">
                {t('auth.sessionExpired')}
              </p>
            )}

            {notice && (
              <section role="status" className="mt-5 grid gap-3 rounded-2xl border border-brand-200/70 bg-brand-soft/60 p-4">
                <div>
                  <h2 className="font-semibold">{t(notice === 'submitted' ? 'auth.registrationSubmittedTitle' : 'auth.pendingApprovalTitle')}</h2>
                  <p className="mt-1 text-sm leading-6 text-ink-soft">
                    {t(notice === 'submitted' ? 'auth.registrationSubmitted' : 'auth.pendingApproval')}
                  </p>
                </div>
                <div className="grid gap-1.5 border-t border-brand-200/50 pt-3">
                  <h3 className="text-sm font-semibold">{t('auth.productInfoTitle')}</h3>
                  <p className="text-sm leading-6 text-ink-soft">{t('auth.productInfoDescription')}</p>
                  <ul className="list-inside list-disc text-sm leading-6 text-ink-soft">
                    <li>{t('auth.productFeaturePayments')}</li>
                    <li>{t('auth.productFeatureRecovery')}</li>
                    <li>{t('auth.productFeatureAi')}</li>
                  </ul>
                  <Link to="/" className="focus-ring mt-1 w-fit rounded text-sm font-semibold text-brand-text">
                    {t('auth.learnMore')}
                  </Link>
                </div>
              </section>
            )}

            <form onSubmit={onSubmit} className="mt-8 grid gap-5" noValidate>
              {mode === 'register' && (
                <Field label={t('auth.displayName')} required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      autoComplete="name"
                      maxLength={100}
                      required
                      value={displayName}
                      invalid={invalid}
                      aria-describedby={describedBy}
                      onChange={(e) => {
                        setDisplayName(e.target.value)
                        setNotice(null)
                        setError(null)
                      }}
                    />
                  )}
                </Field>
              )}
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
                    onChange={(e) => {
                      setEmail(e.target.value)
                      setNotice(null)
                      setError(null)
                    }}
                  />
                )}
              </Field>
              <Field
                label={t('auth.password')}
                required
                error={error ?? undefined}
                hint={mode === 'register' ? t('auth.registrationPasswordHint') : undefined}
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="password"
                    autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
                    required
                    minLength={mode === 'register' ? 12 : undefined}
                    maxLength={128}
                    value={password}
                    invalid={invalid}
                    aria-describedby={describedBy}
                    onChange={(e) => {
                      setPassword(e.target.value)
                      setNotice(null)
                      setError(null)
                    }}
                  />
                )}
              </Field>
              <Button
                type="submit"
                className="mt-1 w-full"
                loading={pending}
                disabled={!email || !password || (mode === 'register' && (!displayName || password.length < 12))}
              >
                {mode === 'login' ? t('auth.submit') : t('auth.registerSubmit')}
              </Button>
            </form>

            <p className="mt-5 text-center text-sm text-ink-soft">
              {mode === 'login' ? t('auth.noAccount') : t('auth.haveAccount')}{' '}
              <button
                type="button"
                disabled={pending}
                className="focus-ring rounded font-semibold text-brand-text disabled:opacity-60"
                onClick={() => {
                  setMode(mode === 'login' ? 'register' : 'login')
                  setNotice(null)
                  setError(null)
                }}
              >
                {mode === 'login' ? t('auth.createAccountLink') : t('auth.backToSignIn')}
              </button>
            </p>
          </div>
        </main>
      </section>
    </div>
  )
}

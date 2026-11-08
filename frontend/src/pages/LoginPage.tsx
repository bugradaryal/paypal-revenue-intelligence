import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { Button, Card, Field, Input, ThemeToggle } from '@/components/ui'
import { LanguageMenu } from '@/components/layout/LanguageMenu'
import { Logo } from '@/components/layout/Logo'
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
  const from = (location.state as { from?: string } | null)?.from ?? '/'

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
    <div className="grid min-h-dvh grid-rows-[auto_1fr] bg-surface-app">
      <header className="flex items-center justify-between px-4 py-4 sm:px-8">
        <Logo />
        <div className="flex items-center gap-2">
          <LanguageMenu />
          <ThemeToggle />
        </div>
      </header>

      <main className="grid place-items-center px-4 pb-16">
        <Card className="w-full max-w-sm">
          <h1 className="text-xl font-bold tracking-tight">{t('auth.title')}</h1>
          <p className="mt-1 text-sm text-ink-soft">{t('auth.description')}</p>

          {getLogoutReason() === 'expired' && !error && (
            <p role="status" className="mt-4 rounded-md bg-surface-muted p-3 text-sm text-ink-soft">
              {t('auth.sessionExpired')}
            </p>
          )}

          <form onSubmit={onSubmit} className="mt-5 grid gap-4" noValidate>
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
            <Button type="submit" loading={pending} disabled={!email || !password}>
              {t('auth.submit')}
            </Button>
          </form>
        </Card>
      </main>
    </div>
  )
}

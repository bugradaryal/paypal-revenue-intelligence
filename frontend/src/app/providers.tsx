import { useEffect, useRef, type ReactNode } from 'react'
import { QueryClientProvider, useQueryClient } from '@tanstack/react-query'
import { ToastProvider } from '@/components/ui'
import { AuthProvider, useAuth } from '@/features/auth/AuthProvider'
import { I18nProvider, useI18n } from '@/i18n'
import { queryClient } from './queryClient'
import { ThemeProvider } from './theme'

/** Server text (e.g. AI explanations) depends on the language, so refetch when it changes. */
function RefetchOnLanguageChange() {
  const { locale } = useI18n()
  const queryClient = useQueryClient()
  const previous = useRef(locale)

  useEffect(() => {
    if (previous.current === locale) return
    previous.current = locale
    void queryClient.invalidateQueries()
  }, [locale, queryClient])

  return null
}

/** Another person must never see the previous session's cached data. */
function ClearCacheOnSignOut() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const hadUser = useRef(Boolean(user))

  useEffect(() => {
    if (hadUser.current && !user) queryClient.clear()
    hadUser.current = Boolean(user)
  }, [user, queryClient])

  return null
}

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <I18nProvider>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <RefetchOnLanguageChange />
            <ClearCacheOnSignOut />
            <ToastProvider>{children}</ToastProvider>
          </AuthProvider>
        </QueryClientProvider>
      </I18nProvider>
    </ThemeProvider>
  )
}

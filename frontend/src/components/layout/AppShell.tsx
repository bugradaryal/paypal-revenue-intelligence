import { Suspense, useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { ErrorBoundary } from '@/app/ErrorBoundary'
import { PageFallback } from '@/app/PageFallback'
import { Dialog } from '@/components/ui'
import { useI18n } from '@/i18n'
import { Logo } from './Logo'
import { SidebarNav } from './SidebarNav'
import { Topbar } from './Topbar'
import { findNavItemByPath } from './navigation'

export function AppShell() {
  const { t } = useI18n()
  const { pathname } = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)

  // Close the mobile drawer after navigating.
  useEffect(() => setMenuOpen(false), [pathname])

  // ...and when the viewport grows to the desktop layout.
  useEffect(() => {
    const mq = window.matchMedia('(min-width: 1024px)')
    const onChange = (e: MediaQueryListEvent) => e.matches && setMenuOpen(false)
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }, [])

  // Document title follows the current page and language.
  const current = findNavItemByPath(pathname)
  const pageTitle = current ? t(current.labelKey) : undefined
  const productName = t('app.product')
  useEffect(() => {
    document.title = pageTitle ? `${pageTitle} · ${productName}` : productName
  }, [pageTitle, productName])

  return (
    <div className="min-h-dvh bg-surface-app">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[70] focus:rounded-md focus:bg-surface-card focus:px-4 focus:py-2 focus:text-sm focus:font-semibold focus:shadow-lg"
      >
        {t('common.skipToContent')}
      </a>

      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 flex-col gap-8 border-r border-line bg-surface-card px-4 py-5 lg:flex">
        <Logo className="px-2" />
        <SidebarNav />
      </aside>

      <div className="lg:pl-64">
        <Topbar onOpenMenu={() => setMenuOpen(true)} />
        <main id="main" tabIndex={-1} className="mx-auto max-w-7xl px-4 py-8 outline-none sm:px-6 lg:px-8">
          <ErrorBoundary key={pathname}>
            <Suspense fallback={<PageFallback />}>
              <Outlet />
            </Suspense>
          </ErrorBoundary>
        </main>
      </div>

      <Dialog
        variant="drawer-left"
        open={menuOpen}
        onClose={() => setMenuOpen(false)}
        title={t('topbar.menu')}
        closeLabel={t('common.close')}
      >
        <SidebarNav onNavigate={() => setMenuOpen(false)} />
      </Dialog>
    </div>
  )
}

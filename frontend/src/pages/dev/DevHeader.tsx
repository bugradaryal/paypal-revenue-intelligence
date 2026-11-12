import { ArrowLeft } from 'lucide-react'
import { Link } from 'react-router'
import { LanguageMenu } from '@/components/layout/LanguageMenu'
import { ThemeToggle } from '@/components/ui'
import { useI18n, type MessageKey } from '@/i18n'
import { cn } from '@/lib/cn'

type DevPage = 'components' | 'tokens' | 'data'

const pages: Array<{ id: DevPage; to: string; labelKey: MessageKey }> = [
  { id: 'components', to: '/design-system', labelKey: 'dev.nav.components' },
  { id: 'tokens', to: '/design-system/tokens', labelKey: 'dev.nav.tokens' },
  { id: 'data', to: '/design-system/data', labelKey: 'dev.nav.data' },
]

/** Shared header of the internal developer pages. Language and theme work here too. */
export function DevHeader({ current }: { current: DevPage }) {
  const { t } = useI18n()

  return (
    <header className="sticky top-0 z-10 border-b border-line bg-surface-app/80 backdrop-blur">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="flex h-16 items-center justify-between gap-3">
          <Link
            to="/"
            className="focus-ring inline-flex items-center gap-2 rounded-sm text-sm font-medium text-ink-soft hover:text-ink"
          >
            <ArrowLeft className="size-4" /> {t('dev.nav.app')}
          </Link>
          <div className="flex items-center gap-2 sm:gap-3">
            <LanguageMenu />
            <ThemeToggle />
          </div>
        </div>
        <nav className="-mt-1 flex pb-3">
          <div className="inline-flex gap-1 rounded-lg bg-surface-muted p-1">
            {pages.map((p) => (
              <Link
                key={p.id}
                to={p.to}
                aria-current={p.id === current ? 'page' : undefined}
                className={cn(
                  'focus-ring h-8 rounded-md px-3.5 text-xs font-semibold leading-8 transition-colors',
                  p.id === current ? 'bg-surface-card text-ink shadow-sm' : 'text-ink-soft hover:text-ink',
                )}
              >
                {t(p.labelKey)}
              </Link>
            ))}
          </div>
        </nav>
      </div>
    </header>
  )
}

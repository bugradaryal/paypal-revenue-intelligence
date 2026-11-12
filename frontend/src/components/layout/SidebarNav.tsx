import { NavLink } from 'react-router'
import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAdminUsers } from '@/features/users/queries'
import { Badge } from '@/components/ui'
import { footerNav, navSections, type NavItem } from './navigation'

function SidebarLink({ item, onNavigate, badge }: { item: NavItem; onNavigate?: () => void; badge?: number }) {
  const { t } = useI18n()
  const Icon = item.icon

  return (
    <NavLink
      to={item.path}
      end={item.path === '/'}
      onClick={onNavigate}
      className={({ isActive }) =>
        cn(
          'focus-ring group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition-[background-color,color,transform,box-shadow] duration-200',
          isActive
            ? 'bg-brand-soft text-brand-text shadow-xs ring-1 ring-brand-500/15'
            : 'text-ink-soft hover:translate-x-0.5 hover:bg-surface-muted hover:text-ink',
        )
      }
    >
      {({ isActive }) => (
        <>
          <Icon
            className={cn('size-[1.15rem] shrink-0 transition-colors', isActive ? 'text-brand-600 dark:text-brand-300' : 'text-ink-faint group-hover:text-ink-soft')}
          />
          <span className="flex min-w-0 flex-1 items-center justify-between gap-2">
            <span className="truncate">{t(item.labelKey)}</span>
            {badge !== undefined && badge > 0 && (
              <Badge size="sm" tone="warning" aria-label={t('users.pendingRequests', { count: badge })}>
                {badge}
              </Badge>
            )}
          </span>
        </>
      )}
    </NavLink>
  )
}

export function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useI18n()
  const { user } = useAuth()
  const users = useAdminUsers(user?.role === 'admin')
  const pendingCount = users.data?.filter((item) => item.isPendingApproval).length ?? 0
  const visibleFooterNav = footerNav.filter((item) => item.page !== 'users' || user?.role === 'admin')

  return (
    <nav aria-label={t('nav.main')} className="flex min-h-0 flex-1 flex-col gap-6">
      {navSections.map((section) => (
        <div key={section.id} className="grid gap-1">
          {section.titleKey && (
            <p className="px-3 pb-1 text-[11px] font-semibold uppercase tracking-wider text-ink-faint">{t(section.titleKey)}</p>
          )}
          {section.items.map((item) => (
            <SidebarLink key={item.path} item={item} onNavigate={onNavigate} />
          ))}
        </div>
      ))}
      <div className="mt-auto grid gap-1 border-t border-line pt-4">
        {visibleFooterNav.map((item) => (
          <SidebarLink
            key={item.path}
            item={item}
            onNavigate={onNavigate}
            badge={item.page === 'users' ? pendingCount : undefined}
          />
        ))}
      </div>
    </nav>
  )
}

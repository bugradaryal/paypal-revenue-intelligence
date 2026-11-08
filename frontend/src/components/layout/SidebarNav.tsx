import { NavLink } from 'react-router'
import { cn } from '@/lib/cn'
import { useI18n } from '@/i18n'
import { footerNav, navSections, type NavItem } from './navigation'

function SidebarLink({ item, onNavigate }: { item: NavItem; onNavigate?: () => void }) {
  const { t } = useI18n()
  const Icon = item.icon

  return (
    <NavLink
      to={item.path}
      end={item.path === '/'}
      onClick={onNavigate}
      className={({ isActive }) =>
        cn(
          'focus-ring group flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium transition-colors duration-150',
          isActive ? 'bg-brand-soft text-brand-text' : 'text-ink-soft hover:bg-surface-muted hover:text-ink',
        )
      }
    >
      {({ isActive }) => (
        <>
          <Icon
            className={cn('size-[1.15rem] shrink-0 transition-colors', isActive ? 'text-brand-500' : 'text-ink-faint group-hover:text-ink-soft')}
          />
          <span className="truncate">{t(item.labelKey)}</span>
        </>
      )}
    </NavLink>
  )
}

export function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const { t } = useI18n()

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
        {footerNav.map((item) => (
          <SidebarLink key={item.path} item={item} onNavigate={onNavigate} />
        ))}
      </div>
    </nav>
  )
}

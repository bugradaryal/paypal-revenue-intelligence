import { LogOut, Menu } from 'lucide-react'
import { useLocation } from 'react-router'
import { Avatar, Dropdown, IconButton, ThemeToggle } from '@/components/ui'
import { useAuth } from '@/features/auth/AuthProvider'
import { MockDataBadge } from '@/components/patterns/MockDataBadge'
import { useI18n } from '@/i18n'
import { LanguageMenu } from './LanguageMenu'
import { LogoMark } from './Logo'
import { findNavItemByPath } from './navigation'

export function Topbar({ onOpenMenu }: { onOpenMenu: () => void }) {
  const { t, tryT } = useI18n()
  const { pathname } = useLocation()
  const current = findNavItemByPath(pathname)
  const { required, user, signOut } = useAuth()
  const displayName = required && user ? user.displayName : t('topbar.demoUser')
  const roleLabel = required && user ? tryT(`auth.role.${user.role}`, user.role) : t('topbar.demoRole')

  return (
    <header className="sticky top-0 z-20 border-b border-line bg-surface-app/80 backdrop-blur">
      <div className="flex h-16 items-center gap-3 px-4 sm:px-6 lg:px-8">
        <IconButton aria-label={t('topbar.openMenu')} className="lg:hidden" onClick={onOpenMenu}>
          <Menu />
        </IconButton>
        <LogoMark className="max-[379px]:hidden lg:hidden" />

        {current && (
          <p className="hidden min-w-0 items-center gap-2 truncate text-sm lg:flex">
            {'sectionTitleKey' in current && current.sectionTitleKey && (
              <>
                <span className="text-ink-faint">{t(current.sectionTitleKey)}</span>
                <span className="text-ink-faint">/</span>
              </>
            )}
            <span className="font-semibold">{t(current.labelKey)}</span>
          </p>
        )}

        <div className="ml-auto flex items-center gap-2 sm:gap-3">
          <MockDataBadge />
          <LanguageMenu />
          <ThemeToggle />
          <div className="hidden items-center gap-3 border-l border-line pl-3 sm:flex">
            <Avatar name={displayName} />
            <div className="hidden leading-tight md:block">
              <p className="text-sm font-semibold">{displayName}</p>
              <p className="text-xs text-ink-faint">{roleLabel}</p>
            </div>
          </div>
          {required && user && (
            <Dropdown
              items={[{ id: 'signout', label: t('auth.signOut'), icon: <LogOut className="size-4" />, onSelect: signOut }]}
              trigger={(props) => (
                <IconButton aria-label={t('auth.userMenu')} {...props}>
                  <LogOut />
                </IconButton>
              )}
            />
          )}
        </div>
      </div>
    </header>
  )
}

import { BellRing, CalendarClock, ChartNoAxesCombined, CreditCard, Gauge, LayoutDashboard, Settings, Sparkles, Users, Workflow, type LucideIcon } from 'lucide-react'
import type { MessageKey } from '@/i18n'

export type PageKey = 'overview' | 'payments' | 'commandCenter' | 'cashFlow' | 'customerCohorts' | 'signals' | 'aiCost' | 'aiWorkspace' | 'users' | 'settings'

export interface NavItem {
  page: PageKey
  path: string
  labelKey: MessageKey
  icon: LucideIcon
}

export interface NavSection {
  id: string
  titleKey?: MessageKey
  items: NavItem[]
}

/** Single source of truth for the sidebar, the topbar breadcrumb and the document title. */
export const navSections: NavSection[] = [
  {
    id: 'main',
    items: [{ page: 'overview', path: '/dashboard', labelKey: 'nav.overview', icon: LayoutDashboard }],
  },
  {
    id: 'revenue',
    titleKey: 'nav.groupRevenue',
    items: [
      { page: 'payments', path: '/payments', labelKey: 'nav.payments', icon: CreditCard },
      { page: 'commandCenter', path: '/command-center', labelKey: 'nav.commandCenter', icon: Workflow },
      { page: 'cashFlow', path: '/cash-flow', labelKey: 'nav.cashFlow', icon: CalendarClock },
      { page: 'customerCohorts', path: '/cohorts', labelKey: 'nav.customerCohorts', icon: ChartNoAxesCombined },
      { page: 'signals', path: '/signals', labelKey: 'nav.signals', icon: BellRing },
    ],
  },
  {
    id: 'ai',
    titleKey: 'nav.groupAi',
    items: [
      { page: 'aiCost', path: '/ai-cost', labelKey: 'nav.aiCost', icon: Gauge },
      { page: 'aiWorkspace', path: '/ai-workspace', labelKey: 'nav.aiWorkspace', icon: Sparkles },
    ],
  },
]

export const footerNav: NavItem[] = [
  { page: 'users', path: '/users', labelKey: 'nav.users', icon: Users },
  { page: 'settings', path: '/settings', labelKey: 'nav.settings', icon: Settings },
]

const allItems: Array<NavItem & { sectionTitleKey?: MessageKey }> = [
  ...navSections.flatMap((s) => s.items.map((i) => ({ ...i, sectionTitleKey: s.titleKey }))),
  ...footerNav,
]

export function findNavItemByPath(pathname: string) {
  const clean = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname
  return allItems.find((i) => i.path === clean)
}

export function findNavItemByPage(page: PageKey) {
  return allItems.find((i) => i.page === page)
}

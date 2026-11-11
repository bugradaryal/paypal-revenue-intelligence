import { FlaskConical } from 'lucide-react'
import { useLocation } from 'react-router'
import { Badge, Tooltip } from '@/components/ui'
import { env } from '@/config/env'
import { useI18n } from '@/i18n'

/**
 * Project rule: sample data must never look real. In real mode only the AI workspace chat is mocked.
 */
export function MockDataBadge() {
  const { t } = useI18n()
  const { pathname } = useLocation()
  const isMockWorkspace = !env.isMock && pathname === '/ai-workspace'

  if (!env.isMock && !isMockWorkspace) return null

  return (
    <Tooltip content={t(isMockWorkspace ? 'topbar.mockWorkspaceHint' : 'topbar.mockDataHint')} side="bottom" wrap>
      <Badge tone="warning" tabIndex={0} className="cursor-help">
        <FlaskConical className="size-3.5" />
        {t(isMockWorkspace ? 'topbar.mockWorkspace' : 'topbar.mockData')}
      </Badge>
    </Tooltip>
  )
}

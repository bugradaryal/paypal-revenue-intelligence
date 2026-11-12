import { FlaskConical } from 'lucide-react'
import { Badge, Tooltip } from '@/components/ui'
import { env } from '@/config/env'
import { useI18n } from '@/i18n'

/**
 * Project rule: sample data must never look real. The badge is shown only when the app uses the mock adapter.
 */
export function MockDataBadge() {
  const { t } = useI18n()

  if (!env.isMock) return null

  return (
    <Tooltip content={t('topbar.mockDataHint')} side="bottom" wrap>
      <Badge tone="warning" tabIndex={0} className="cursor-help">
        <FlaskConical className="size-3.5" />
        {t('topbar.mockData')}
      </Badge>
    </Tooltip>
  )
}

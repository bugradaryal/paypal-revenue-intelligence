import { FlaskConical } from 'lucide-react'
import { Badge, Tooltip } from '@/components/ui'
import { useI18n } from '@/i18n'
import { hasMockOperations } from '@/services/adapters'

/**
 * Project rule: sample data must never look real. Renders while any shown data comes from the mock adapter.
 */
export function MockDataBadge() {
  const { t } = useI18n()
  if (!hasMockOperations) return null

  return (
    <Tooltip content={t('topbar.mockDataHint')} side="bottom" wrap>
      <Badge tone="warning" tabIndex={0} className="cursor-help">
        <FlaskConical className="size-3.5" />
        {t('topbar.mockData')}
      </Badge>
    </Tooltip>
  )
}

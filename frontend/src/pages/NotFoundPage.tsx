import { Link } from 'react-router'
import { buttonStyles, Card } from '@/components/ui'
import { useI18n } from '@/i18n'

export function NotFoundPage() {
  const { t } = useI18n()

  return (
    <Card className="mx-auto mt-10 grid max-w-lg place-items-center gap-3 py-16 text-center">
      <p className="tabular text-5xl font-medium text-brand-500">404</p>
      <h1 className="text-xl font-bold tracking-tight">{t('notFound.title')}</h1>
      <p className="text-sm text-ink-soft">{t('notFound.description')}</p>
      <Link to="/" className={buttonStyles({ variant: 'soft', className: 'mt-3' })}>
        {t('notFound.back')}
      </Link>
    </Card>
  )
}

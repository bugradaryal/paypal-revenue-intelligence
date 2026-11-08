import { Check, Globe } from 'lucide-react'
import { Button, Dropdown } from '@/components/ui'
import { LOCALES, useI18n } from '@/i18n'

export function LanguageMenu() {
  const { locale, setLocale, t } = useI18n()

  return (
    <Dropdown
      trigger={(props) => (
        <Button
          variant="ghost"
          size="sm"
          aria-label={t('topbar.language')}
          leadingIcon={<Globe className="size-4" />}
          className="h-9 px-2.5 uppercase"
          {...props}
        >
          <span className="max-[379px]:hidden">{locale}</span>
        </Button>
      )}
      items={LOCALES.map((l) => ({
        id: l.code,
        label: l.label,
        icon: l.code === locale ? <Check className="text-brand-500" /> : <span className="size-4" />,
        onSelect: () => setLocale(l.code),
      }))}
    />
  )
}

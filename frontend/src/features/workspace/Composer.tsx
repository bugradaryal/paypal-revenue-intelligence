import { useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { ArrowUp, Square } from 'lucide-react'
import { Button, IconButton, Select } from '@/components/ui'
import { useI18n } from '@/i18n'
import type { ChatProviderSelection } from '@/services/types'

const MAX_LENGTH = 4000
const PROVIDERS = [
  { value: 'auto', label: 'providerAuto' },
  { value: 'openai', label: 'providerOpenai' },
  { value: 'gemini', label: 'providerGemini' },
] as const

export function Composer({
  streaming,
  provider,
  onProviderChange,
  onSend,
  onStop,
}: {
  streaming: boolean
  provider: ChatProviderSelection
  onProviderChange: (provider: ChatProviderSelection) => void
  onSend: (text: string) => void
  onStop: () => void
}) {
  const { t } = useI18n()
  const [value, setValue] = useState('')
  const ref = useRef<HTMLTextAreaElement>(null)

  function submit(event?: FormEvent) {
    event?.preventDefault()
    if (!value.trim() || streaming) return
    onSend(value)
    setValue('')
    if (ref.current) ref.current.style.height = ''
  }

  function onKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    // Enter sends, Shift+Enter adds a line. Never send while an IME composition is open.
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) {
      e.preventDefault()
      submit()
    }
  }

  const providerOptions = PROVIDERS.map((option) => ({
    value: option.value,
    label: t(`workspace.${option.label}`),
  }))

  return (
    <div>
      <div className="mb-1 flex justify-end pr-2">
        <Select
          id="workspace-provider"
          aria-label={t('workspace.providerLabel')}
          options={providerOptions}
          value={provider}
          disabled={streaming}
          onChange={(event) => {
            const selected = PROVIDERS.find((option) => option.value === event.currentTarget.value)
            if (selected) onProviderChange(selected.value)
          }}
          className="h-8 min-w-32 text-xs"
        />
      </div>
      <form onSubmit={submit} className="rounded-2xl border border-line bg-surface-card p-2 shadow-md focus-within:border-brand-400">
        <label htmlFor="workspace-input" className="sr-only">
          {t('workspace.inputLabel')}
        </label>
        <textarea
          id="workspace-input"
          ref={ref}
          rows={1}
          maxLength={MAX_LENGTH}
          value={value}
          placeholder={t('workspace.placeholder')}
          onKeyDown={onKeyDown}
          onChange={(e) => {
            setValue(e.target.value)
            e.target.style.height = 'auto'
            e.target.style.height = `${Math.min(e.target.scrollHeight, 180)}px`
          }}
          className="block max-h-44 w-full resize-none bg-transparent px-3 py-2 text-sm outline-none placeholder:text-ink-faint"
        />
        <div className="flex items-center justify-between gap-3 px-2 pb-1 pt-1">
          <p className="text-[11px] text-ink-faint">{t('workspace.disclaimer')}</p>
          {streaming ? (
            <Button type="button" size="sm" variant="secondary" leadingIcon={<Square className="size-3.5 fill-current" />} onClick={onStop}>
              {t('workspace.stop')}
            </Button>
          ) : (
            <IconButton type="submit" aria-label={t('workspace.send')} disabled={!value.trim()} className="bg-brand-500 text-ink-on-brand hover:bg-brand-600 disabled:bg-surface-muted disabled:text-ink-faint">
              <ArrowUp />
            </IconButton>
          )}
        </div>
      </form>
    </div>
  )
}

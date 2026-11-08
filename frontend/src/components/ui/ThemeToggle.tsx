import { Moon, Sun } from 'lucide-react'
import { useTheme } from '@/app/theme'
import { useI18n } from '@/i18n'
import { cn } from '@/lib/cn'

export function ThemeToggle({ className }: { className?: string }) {
  const { theme, toggleTheme } = useTheme()
  const { t } = useI18n()
  const isDark = theme === 'dark'

  return (
    <button
      type="button"
      role="switch"
      aria-checked={isDark}
      aria-label={isDark ? t('theme.toLight') : t('theme.toDark')}
      onClick={toggleTheme}
      className={cn(
        'focus-ring relative inline-flex h-9 w-[4.25rem] shrink-0 items-center rounded-full border border-line bg-surface-muted p-1',
        'shadow-xs transition-colors duration-200 hover:border-line-strong',
        className,
      )}
    >
      <span
        className={cn(
          'absolute left-1 top-1 grid size-7 place-items-center rounded-full bg-brand-gradient text-ink-on-brand shadow-brand',
          'transition-transform duration-300 ease-[var(--ease-out-soft)]',
          isDark && 'translate-x-[2rem]',
        )}
      >
        {isDark ? <Moon className="size-3.5" strokeWidth={2.4} /> : <Sun className="size-3.5" strokeWidth={2.4} />}
      </span>
      <Sun className="relative ml-[0.45rem] size-3.5 text-ink-faint" aria-hidden />
      <Moon className="relative ml-auto mr-[0.45rem] size-3.5 text-ink-faint" aria-hidden />
    </button>
  )
}

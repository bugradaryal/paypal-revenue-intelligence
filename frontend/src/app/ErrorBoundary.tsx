import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Button, Card } from '@/components/ui'
import { useI18n } from '@/i18n'

function CrashFallback({ onRetry }: { onRetry: () => void }) {
  const { t } = useI18n()
  return (
    <Card role="alert" className="mx-auto mt-10 grid max-w-lg gap-3 py-12 text-center">
      <h1 className="text-xl font-bold tracking-tight">{t('crash.title')}</h1>
      <p className="text-sm text-ink-soft">{t('crash.description')}</p>
      <div className="mt-2 flex justify-center">
        <Button variant="soft" onClick={onRetry}>
          {t('common.retry')}
        </Button>
      </div>
    </Card>
  )
}

/** Catches render errors (including a failed lazy chunk) so one broken screen never blanks the app. */
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }

  static getDerivedStateFromError() {
    return { failed: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(error, info.componentStack)
  }

  render() {
    return this.state.failed ? <CrashFallback onRetry={() => this.setState({ failed: false })} /> : this.props.children
  }
}

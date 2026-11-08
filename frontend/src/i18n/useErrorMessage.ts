import { useCallback } from 'react'
import { ApiError, ContractNotReadyError, NetworkError, TimeoutError } from '@/services/errors'
import { useI18n } from './index'

/**
 * Turns any thrown error into a message in the active language.
 * The UI never shows raw server text: it translates by error class, then by the backend's stable `code`.
 */
export function useErrorMessage() {
  const { t, tryT } = useI18n()

  return useCallback(
    (error: unknown): string => {
      if (error instanceof ContractNotReadyError) return t('errors.contractNotReady')
      if (error instanceof NetworkError) return t('errors.network')
      if (error instanceof TimeoutError) return t('errors.timeout')

      if (error instanceof ApiError) {
        const byCode = error.code ? tryT(`errors.codes.${error.code}`, '') : ''
        if (byCode) return byCode
        if (error.status === 404) return t('errors.notFound')
        if (error.status === 409) return t('errors.conflict')
        if (error.status >= 500) return t('errors.server')
      }
      return t('errors.generic')
    },
    [t, tryT],
  )
}

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { en, type Messages } from './locales/en'
import { de } from './locales/de'
import { tr } from './locales/tr'
import { es } from './locales/es'
import { setCurrentLocale } from './current'

/**
 * Small, dependency-free i18n. Keys are type-checked against the English dictionary.
 * Numbers, currency and dates are formatted with Intl using `intlLocale`.
 */
export const LOCALES = [
  { code: 'en', label: 'English', intl: 'en-US' },
  { code: 'de', label: 'Deutsch', intl: 'de-DE' },
  { code: 'tr', label: 'Türkçe', intl: 'tr-TR' },
  { code: 'es', label: 'Español', intl: 'es-ES' },
] as const

export type Locale = (typeof LOCALES)[number]['code']

const dictionaries: Record<Locale, Messages> = { en, de, tr, es }

type Leaves<T, P extends string = ''> = {
  [K in keyof T & string]: T[K] extends string ? `${P}${K}` : Leaves<T[K], `${P}${K}.`>
}[keyof T & string]

export type MessageKey = Leaves<Messages>

type Params = Record<string, string | number>

const STORAGE_KEY = 'pri-lang'

function isLocale(value: unknown): value is Locale {
  return LOCALES.some((l) => l.code === value)
}

function detectInitialLocale(): Locale {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (isLocale(saved)) return saved
  } catch {
    /* storage unavailable */
  }
  const preferred = navigator.languages?.length ? navigator.languages : [navigator.language]
  for (const tag of preferred) {
    const base = tag.toLowerCase().split('-')[0]
    if (isLocale(base)) return base
  }
  return 'en'
}

function resolve(dict: Messages, key: string): string | undefined {
  let current: unknown = dict
  for (const part of key.split('.')) {
    if (typeof current !== 'object' || current === null) return undefined
    current = (current as Record<string, unknown>)[part]
  }
  return typeof current === 'string' ? current : undefined
}

interface I18nContextValue {
  locale: Locale
  intlLocale: string
  setLocale: (locale: Locale) => void
  t: (key: MessageKey, params?: Params) => string
  /**
   * For keys built from backend values (status, failure code, error code). Returns `fallback`
   * when the backend sends a value the dictionary does not know yet, instead of showing a raw key.
   */
  tryT: (key: string, fallback: string, params?: Params) => string
}

const I18nContext = createContext<I18nContextValue | null>(null)

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(detectInitialLocale)

  useEffect(() => {
    setCurrentLocale(locale)
    document.documentElement.lang = locale
    try {
      localStorage.setItem(STORAGE_KEY, locale)
    } catch {
      /* storage unavailable */
    }
  }, [locale])

  const setLocale = useCallback((next: Locale) => setLocaleState(next), [])

  const translate = useCallback(
    (key: string, params?: Params): string | undefined => {
      const raw = resolve(dictionaries[locale], key) ?? resolve(en, key)
      if (raw === undefined || !params) return raw
      return raw.replace(/\{(\w+)\}/g, (_, name: string) => String(params[name] ?? `{${name}}`))
    },
    [locale],
  )

  const t = useCallback((key: MessageKey, params?: Params) => translate(key, params) ?? key, [translate])
  const tryT = useCallback(
    (key: string, fallback: string, params?: Params) => translate(key, params) ?? fallback,
    [translate],
  )

  const value = useMemo<I18nContextValue>(
    () => ({
      locale,
      intlLocale: LOCALES.find((l) => l.code === locale)?.intl ?? 'en-US',
      setLocale,
      t,
      tryT,
    }),
    [locale, setLocale, t, tryT],
  )

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n() {
  const ctx = useContext(I18nContext)
  if (!ctx) throw new Error('useI18n must be used inside <I18nProvider>')
  return ctx
}

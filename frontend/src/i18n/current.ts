/**
 * The active language, readable outside React. The HTTP client sends it as `Accept-Language`
 * and the mock adapter localizes its AI-written text with it. Kept in sync by I18nProvider.
 */
let current = 'en'

export const getCurrentLocale = () => current
export const setCurrentLocale = (locale: string) => {
  current = locale
}

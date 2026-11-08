/**
 * The ONLY place environment variables are read.
 * API addresses must never be hard-coded in any other file.
 */
const dataSource = import.meta.env.VITE_DATA_SOURCE === 'real' ? 'real' : 'mock'

export const env = {
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000/api/v1',
  dataSource,
  isMock: dataSource === 'mock',
} as const

import { env } from '@/config/env'
import { mockApi } from './mock'
import { realApi } from './real'
import type { Api } from './types'

/**
 * The only data source the UI talks to. The choice is made by VITE_DATA_SOURCE in .env:
 *
 * - mock: everything is sample data, no login.
 * - real: operations call the backend.
 */

export const api: Api = env.isMock ? mockApi : realApi

/** True when the local demo adapter is selected. */
export const hasMockOperations: boolean = env.isMock

export type { Api, AiCostApi, RevenueApi } from './types'
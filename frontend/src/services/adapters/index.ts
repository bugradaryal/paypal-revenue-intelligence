import { env } from '@/config/env'
import { mockApi } from './mock'
import { realApi } from './real'
import type { Api } from './types'

/**
 * The only data source the UI talks to. The choice is made by VITE_DATA_SOURCE in .env:
 *
 * - mock: everything is sample data, no login.
 * - real: everything calls the backend, except the operations listed in MOCK_ONLY below.
 *
 * No other list needs to be maintained when a backend endpoint becomes ready.
 */

/**
 * Operations that stay on sample data even in real mode, because the backend has no usable
 * endpoint for them yet. Empty this list (one line at a time) when the contract defines them.
 * AI Workspace streaming: the exact event format is only defined in contract 0.4.
 */
const MOCK_ONLY: ReadonlySet<string> = new Set<string>(['workspace.streamChat'])

type OperationGroups = Record<string, Record<string, unknown>>

function compose(): Api {
  if (env.isMock) return mockApi

  const real = realApi as unknown as OperationGroups
  const mock = mockApi as unknown as OperationGroups
  const out: OperationGroups = {}

  for (const group of Object.keys(real)) {
    out[group] = {}
    for (const operation of Object.keys(real[group] ?? {})) {
      out[group][operation] = (MOCK_ONLY.has(`${group}.${operation}`) ? mock : real)[group]?.[operation]
    }
  }
  return out as unknown as Api
}

export const api: Api = compose()

/** True while any shown data comes from the mock adapter. Drives the "Mock data" badge. */
export const hasMockOperations: boolean = env.isMock || MOCK_ONLY.size > 0

export type { Api, AiCostApi, RevenueApi } from './types'
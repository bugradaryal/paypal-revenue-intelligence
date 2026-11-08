import { env } from '@/config/env'
import { mockApi } from './mock'
import { realApi } from './real'
import type { Api } from './types'

/**
 * The only data source the UI talks to.
 *
 * VITE_DATA_SOURCE=mock: everything is mock.
 * VITE_DATA_SOURCE=real: operations listed in READY call the backend; every other operation stays on the
 * mock adapter, exactly as docs/API_CONTRACT.md prescribes ("Planned" endpoints use the mock).
 *
 * When the contract moves an endpoint to "Ready", add its operation name here. Nothing else changes.
 */
const READY: ReadonlySet<string> = new Set<string>([
  // The contract's "Ready" list has no data operations yet (only health, ping and auth).
])

type OperationGroups = Record<string, Record<string, unknown>>

function compose(): Api {
  if (env.isMock) return mockApi

  const real = realApi as unknown as OperationGroups
  const mock = mockApi as unknown as OperationGroups
  const out: OperationGroups = {}

  for (const group of Object.keys(real)) {
    out[group] = {}
    for (const operation of Object.keys(real[group] ?? {})) {
      out[group][operation] = (READY.has(`${group}.${operation}`) ? real : mock)[group]?.[operation]
    }
  }
  return out as unknown as Api
}

export const api: Api = compose()

/** True while any shown data comes from the mock adapter. Drives the "Mock data" badge. */
export const hasMockOperations: boolean =
  env.isMock ||
  Object.entries(realApi as unknown as OperationGroups).some(([group, ops]) =>
    Object.keys(ops).some((operation) => !READY.has(`${group}.${operation}`)),
  )

export type { Api, AiCostApi, RevenueApi } from './types'

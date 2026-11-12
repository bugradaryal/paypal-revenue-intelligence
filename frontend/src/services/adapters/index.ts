import { env } from '@/config/env'
import { mockApi } from './mock'
import { buildMockWorkspaceContext } from './mock/workspace'
import { realApi } from './real'
import type { Api } from './types'

/**
 * The only API surface the UI talks to. Data and AI sources can be selected independently:
 *
 * - VITE_DATA_SOURCE=mock: business data uses the local sample dataset.
 * - VITE_AI_SOURCE=real (default): workspace chat uses the authenticated backend.
 */

const dataApi: Api = env.isMock ? mockApi : realApi
const workspace: Api['workspace'] =
  env.aiSource === 'real'
    ? env.isMock
      ? {
          streamChat: (request, options) =>
            realApi.workspace.streamChat(
              { ...request, context: buildMockWorkspaceContext() },
              options,
            ),
        }
      : realApi.workspace
    : mockApi.workspace

export const api: Api = { ...dataApi, workspace }

/** True when the local demo adapter is selected. */
export const hasMockOperations: boolean = env.isMock

export type { Api, AiCostApi, RevenueApi } from './types'
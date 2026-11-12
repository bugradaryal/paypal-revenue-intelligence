import { useCallback, useEffect, useRef, useState } from 'react'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import { api } from '@/services/adapters'
import { isAbortError } from '@/services/errors'
import type { ChatEvent, ChatUsage, RecoverySuggestion } from '@/services/types'

/**
 * The conversation lives here, in the client: the backend stores nothing, so every request carries the
 * history (contract, "AI Workspace"). It is kept in sessionStorage so a reload does not lose it,
 * and it ends with the browser tab.
 */

export interface ChatTurn {
  id: string
  role: 'user' | 'assistant'
  content: string
  status: 'streaming' | 'done' | 'stopped' | 'error'
  /** Already translated, for status "error". */
  error?: string
  suggestions: RecoverySuggestion[]
  usage?: ChatUsage
}

const KEY = 'pri-workspace'

function load(): ChatTurn[] {
  try {
    const raw = sessionStorage.getItem(KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw) as ChatTurn[]
    // A turn that was streaming when the page closed was cut off.
    return parsed.map((turn) => (turn.status === 'streaming' ? { ...turn, status: 'stopped' as const } : turn))
  } catch {
    return []
  }
}

const newId = () => `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`

export function useWorkspaceChat() {
  const { t, tryT } = useI18n()
  const errorMessage = useErrorMessage()
  const [turns, setTurns] = useState<ChatTurn[]>(load)
  const [model, setModel] = useState('auto')
  const turnsRef = useRef(turns)
  const abortRef = useRef<AbortController | null>(null)

  useEffect(() => {
    turnsRef.current = turns
    try {
      sessionStorage.setItem(KEY, JSON.stringify(turns))
    } catch {
      /* storage unavailable: the conversation then lasts until reload */
    }
  }, [turns])

  const streaming = turns.some((turn) => turn.status === 'streaming')

  const patch = useCallback((id: string, change: (turn: ChatTurn) => ChatTurn) => {
    setTurns((all) => all.map((turn) => (turn.id === id ? change(turn) : turn)))
  }, [])

  const run = useCallback(
    async (history: ChatTurn[], assistantId: string, selectedModel: string) => {
      const controller = new AbortController()
      abortRef.current = controller

      const apply = (event: ChatEvent) => {
        if (event.type === 'token') patch(assistantId, (turn) => ({ ...turn, content: turn.content + event.text }))
        else if (event.type === 'suggestion') patch(assistantId, (turn) => ({ ...turn, suggestions: [...turn.suggestions, event.suggestion] }))
        else if (event.type === 'usage') {
          const { type: _type, ...usage } = event
          patch(assistantId, (turn) => ({ ...turn, usage }))
        } else if (event.type === 'error') {
          patch(assistantId, (turn) => ({
            ...turn,
            status: 'error',
            error: tryT(`errors.codes.${event.code}`, t('errors.generic')),
          }))
        } else if (event.type === 'done') patch(assistantId, (turn) => (turn.status === 'streaming' ? { ...turn, status: 'done' } : turn))
      }

      try {
        const messages = history
          .filter((turn) => turn.content && turn.status !== 'error')
          .map((turn) => ({ role: turn.role, content: turn.content }))

        for await (const event of api.workspace.streamChat({ messages, model: selectedModel }, { signal: controller.signal })) apply(event)
        // A stream that ends without "done" still ends the turn.
        patch(assistantId, (turn) => (turn.status === 'streaming' ? { ...turn, status: 'done' } : turn))
      } catch (error) {
        if (isAbortError(error)) patch(assistantId, (turn) => ({ ...turn, status: 'stopped' }))
        else patch(assistantId, (turn) => ({ ...turn, status: 'error', error: errorMessage(error) }))
      } finally {
        if (abortRef.current === controller) abortRef.current = null
      }
    },
    [errorMessage, patch, t, tryT],
  )

  const send = useCallback(
    (text: string) => {
      const content = text.trim()
      if (!content || turnsRef.current.some((turn) => turn.status === 'streaming')) return
      const user: ChatTurn = { id: newId(), role: 'user', content, status: 'done', suggestions: [] }
      const assistant: ChatTurn = { id: newId(), role: 'assistant', content: '', status: 'streaming', suggestions: [] }
      const history = [...turnsRef.current, user]
      setTurns([...history, assistant])
      void run(history, assistant.id, model)
    },
    [model, run],
  )

  /** Asks the same question again, replacing the last answer. */
  const regenerate = useCallback(() => {
    const current = turnsRef.current
    if (current.some((turn) => turn.status === 'streaming')) return
    const last = current[current.length - 1]
    if (!last || last.role !== 'assistant') return
    const history = current.slice(0, -1)
    const assistant: ChatTurn = { id: newId(), role: 'assistant', content: '', status: 'streaming', suggestions: [] }
    setTurns([...history, assistant])
    void run(history, assistant.id, model)
  }, [model, run])

  const stop = useCallback(() => abortRef.current?.abort(), [])

  const reset = useCallback(() => {
    abortRef.current?.abort()
    setTurns([])
  }, [])

  /** Keeps a proposed action in the conversation in step with the server (after a decision or a status refresh). */
  const syncSuggestion = useCallback((updated: RecoverySuggestion) => {
    setTurns((all) =>
      all.map((turn) =>
        turn.suggestions.some((s) => s.id === updated.id)
          ? { ...turn, suggestions: turn.suggestions.map((s) => (s.id === updated.id ? updated : s)) }
          : turn,
      ),
    )
  }, [])

  return { turns, streaming, model, setModel, send, stop, regenerate, reset, syncSuggestion }
}

import type { Question } from '../api/client'

/// Turns the server's "correct answer" payload into a readable line for feedback and review.
export function formatCorrect(question: Question, correct: unknown): string {
  const c = (correct ?? {}) as Record<string, unknown>
  if (Array.isArray(c.selected)) {
    return (c.selected as string[]).map((id) => question.options?.find((o) => o.id === id)?.text ?? id).join('; ')
  }
  if (typeof c.value === 'boolean') return c.value ? 'True' : 'False'
  if (Array.isArray(c.order)) return (c.order as number[]).map((i) => question.steps?.[i] ?? String(i)).join(' → ')
  if (Array.isArray(c.pairs)) return (c.pairs as { left: string; right: string }[]).map((p) => `${p.left} → ${p.right}`).join('; ')
  if (Array.isArray(c.keywords)) return (c.keywords as string[][]).map((group) => group.join(', ')).join('; ')
  return ''
}

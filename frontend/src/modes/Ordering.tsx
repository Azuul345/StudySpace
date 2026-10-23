import { useState } from 'react'
import { Submit } from './Submit'
import type { ModeProps } from './types'

/// Reorder with up/down buttons (keyboard and screen reader friendly). Submits step indexes in the chosen order.
export function Ordering({ question, onSubmit, disabled }: ModeProps) {
  const steps = question.steps!
  const [order, setOrder] = useState(() => steps.map((_, i) => i))

  const move = (position: number, delta: number) =>
    setOrder((current) => {
      const next = [...current]
      const other = position + delta
      const held = next[position]
      next[position] = next[other]
      next[other] = held
      return next
    })

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit({ order })
      }}
    >
      <fieldset disabled={disabled}>
        <legend>{question.prompt}</legend>
        <ol>
          {order.map((stepIndex, position) => (
            <li key={stepIndex}>
              {steps[stepIndex]}{' '}
              <button type="button" aria-label={`Move ${steps[stepIndex]} up`} disabled={position === 0} onClick={() => move(position, -1)}>
                ↑
              </button>
              <button
                type="button"
                aria-label={`Move ${steps[stepIndex]} down`}
                disabled={position === order.length - 1}
                onClick={() => move(position, 1)}
              >
                ↓
              </button>
            </li>
          ))}
        </ol>
      </fieldset>
      <Submit ready disabled={disabled} />
    </form>
  )
}

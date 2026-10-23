import { useState } from 'react'
import { Submit } from './Submit'
import type { ModeProps } from './types'

export function Matching({ question, onSubmit, disabled }: ModeProps) {
  const { lefts, rights } = question.pairs!
  const [choice, setChoice] = useState<Record<string, string>>({})
  const complete = lefts.every((l) => choice[l])

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit({ pairs: lefts.map((left) => ({ left, right: choice[left] })) })
      }}
    >
      <fieldset disabled={disabled}>
        <legend>{question.prompt}</legend>
        {lefts.map((left) => (
          <div key={left} className="option">
            <label>
              {left}
              <select value={choice[left] ?? ''} onChange={(e) => setChoice({ ...choice, [left]: e.target.value })}>
                <option value="" disabled>
                  Choose…
                </option>
                {rights.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </label>
          </div>
        ))}
      </fieldset>
      <Submit ready={complete} disabled={disabled} />
    </form>
  )
}

import { useState } from 'react'
import { Submit } from './Submit'
import type { ModeProps } from './types'

export function TrueFalse({ question, onSubmit, disabled }: ModeProps) {
  const [value, setValue] = useState<boolean | null>(null)
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        if (value !== null) onSubmit({ value })
      }}
    >
      <fieldset disabled={disabled}>
        <legend>{question.prompt}</legend>
        {[true, false].map((v) => (
          <label key={String(v)} className="option">
            <input type="radio" name={`q${question.id}`} checked={value === v} onChange={() => setValue(v)} />
            {v ? 'True' : 'False'}
          </label>
        ))}
      </fieldset>
      <Submit ready={value !== null} disabled={disabled} />
    </form>
  )
}

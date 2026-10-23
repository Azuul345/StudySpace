import { useState } from 'react'
import { Submit } from './Submit'
import type { ModeProps } from './types'

/// Shared by multiple choice (one pick, radios) and multi-response / scenario (many picks, checkboxes).
export function OptionList({ question, onSubmit, disabled, many }: ModeProps & { many: boolean }) {
  const [selected, setSelected] = useState<string[]>([])
  const toggle = (id: string) =>
    setSelected((current) => (many ? (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]) : [id]))

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit({ selected: question.options!.map((o) => o.id).filter((id) => selected.includes(id)) })
      }}
    >
      <fieldset disabled={disabled}>
        <legend>{question.prompt}</legend>
        {many && <p><small>Select all that apply.</small></p>}
        {question.options!.map((o) => (
          <label key={o.id} className="option">
            <input
              type={many ? 'checkbox' : 'radio'}
              name={`q${question.id}`}
              checked={selected.includes(o.id)}
              onChange={() => toggle(o.id)}
            />
            {o.text}
          </label>
        ))}
      </fieldset>
      <Submit ready={selected.length > 0} disabled={disabled} />
    </form>
  )
}

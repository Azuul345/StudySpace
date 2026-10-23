import { useState } from 'react'
import { Submit } from './Submit'
import type { ModeProps } from './types'

// Mirrors QuestionGrader.MaxTypedLength on the server.
export const MAX_TYPED_LENGTH = 2000

export function TypedAnswer({ question, onSubmit, disabled }: ModeProps) {
  const [text, setText] = useState('')
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit({ text: text.trim() })
      }}
    >
      <label>
        {question.prompt}
        <textarea value={text} maxLength={MAX_TYPED_LENGTH} disabled={disabled} onChange={(e) => setText(e.target.value)} />
      </label>
      <Submit ready={text.trim().length > 0} disabled={disabled} />
    </form>
  )
}

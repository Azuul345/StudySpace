import type { Answer, Question } from '../api/client'

export interface ModeProps {
  question: Question
  onSubmit: (answer: Answer) => void
  /// True once answered (or while submitting): inputs are locked.
  disabled?: boolean
}

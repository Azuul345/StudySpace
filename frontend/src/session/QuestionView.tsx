import type { Question } from '../api/client'
import { Matching } from '../modes/Matching'
import { MultipleChoice } from '../modes/MultipleChoice'
import { MultiResponse } from '../modes/MultiResponse'
import { Ordering } from '../modes/Ordering'
import { Scenario } from '../modes/Scenario'
import { TrueFalse } from '../modes/TrueFalse'
import { TypedAnswer } from '../modes/TypedAnswer'
import type { ModeProps } from '../modes/types'

const MODES: Record<Question['type'], (props: ModeProps) => React.JSX.Element> = {
  mcq: MultipleChoice,
  multi: MultiResponse,
  scenario: Scenario,
  truefalse: TrueFalse,
  ordering: Ordering,
  matching: Matching,
  typed: TypedAnswer,
}

export function QuestionView(props: ModeProps) {
  const Mode = MODES[props.question.type]
  return <Mode {...props} />
}

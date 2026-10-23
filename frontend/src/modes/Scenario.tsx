import { OptionList } from './OptionList'
import type { ModeProps } from './types'

/// A scenario question: the shared scenario text, then multi-response options.
export function Scenario(props: ModeProps) {
  return (
    <>
      {props.question.scenario && <blockquote className="scenario">{props.question.scenario.text}</blockquote>}
      <OptionList {...props} many />
    </>
  )
}

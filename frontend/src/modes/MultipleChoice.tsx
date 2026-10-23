import { OptionList } from './OptionList'
import type { ModeProps } from './types'

export const MultipleChoice = (props: ModeProps) => <OptionList {...props} many={false} />

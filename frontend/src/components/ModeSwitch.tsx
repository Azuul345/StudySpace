import type { StudyMode } from './studyMode'

/// Switches between the professional wording and the plain-language wording.
export function ModeSwitch({ mode, onChange }: { mode: StudyMode; onChange: (mode: StudyMode) => void }) {
  return (
    <div className="mode-switch" role="group" aria-label="Explanation style">
      <button type="button" aria-pressed={mode === 'professional'} onClick={() => onChange('professional')}>
        Professional
      </button>{' '}
      <button type="button" aria-pressed={mode === 'simple'} onClick={() => onChange('simple')}>
        Simple
      </button>
    </div>
  )
}

import { useState } from 'react'

export type StudyMode = 'professional' | 'simple'

const KEY = 'studyspace.studyMode'

/// The explanation style is a per-browser convenience, so storage can fail (private windows, blocked storage).
export function readMode(): StudyMode {
  try {
    return localStorage.getItem(KEY) === 'simple' ? 'simple' : 'professional'
  } catch {
    return 'professional'
  }
}

export function writeMode(mode: StudyMode): void {
  try {
    localStorage.setItem(KEY, mode)
  } catch {
    // Storage is unavailable: the choice still applies until the page reloads.
  }
}

export function useStudyMode(): [StudyMode, (mode: StudyMode) => void] {
  const [mode, setMode] = useState<StudyMode>(readMode)
  const change = (next: StudyMode) => {
    writeMode(next)
    setMode(next)
  }
  return [mode, change]
}

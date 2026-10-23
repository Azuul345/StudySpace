import type { ReactNode } from 'react'
import type { Loaded } from './useLoad'

export function Async<T>({ state, children }: { state: Loaded<T>; children: (data: T) => ReactNode }) {
  if (state.status === 'loading') return <p aria-busy="true">Loading…</p>
  if (state.status === 'error') return <p role="alert" className="error">{state.message}</p>
  return <>{children(state.data)}</>
}

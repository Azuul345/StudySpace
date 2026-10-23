import { useEffect, useState } from 'react'
import { ApiError } from '../api/client'

export type Loaded<T> = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; data: T }

/// `load` must be stable (module function or useCallback) or the effect re-runs on every render.
/// A result only counts for the `load` that produced it, so changing `load` shows "loading" again.
export function useLoad<T>(load: () => Promise<T>): Loaded<T> {
  const [result, setResult] = useState<{ load: () => Promise<T>; value: Loaded<T> } | null>(null)

  useEffect(() => {
    let cancelled = false
    load().then(
      (data) => !cancelled && setResult({ load, value: { status: 'ready', data } }),
      (error: unknown) =>
        !cancelled &&
        setResult({
          load,
          value: { status: 'error', message: error instanceof ApiError ? error.message : 'Something went wrong.' },
        }),
    )
    return () => {
      cancelled = true
    }
  }, [load])

  return result?.load === load ? result.value : { status: 'loading' }
}

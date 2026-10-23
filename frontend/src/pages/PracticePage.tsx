import { useCallback, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { ApiError, createAttempt } from '../api/client'
import type { Attempt, CreateAttempt, PracticeForm } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'
import { Session } from '../session/Session'

const FORMS: PracticeForm[] = ['full', 'drill', 'daily', 'bank', 'scenario']

function numberParam(params: URLSearchParams, name: string): number | undefined {
  const value = params.get(name)
  return value !== null && /^\d+$/.test(value) ? Number(value) : undefined
}

export function PracticePage() {
  const [params] = useSearchParams()
  const key = params.toString()
  // StrictMode runs effects twice in dev; keep one in-flight request per query so only one attempt is created.
  const started = useRef<{ key: string; promise: Promise<Attempt> } | null>(null)

  const load = useCallback((): Promise<Attempt> => {
    if (started.current?.key === key) return started.current.promise
    const form = params.get('form') as PracticeForm | null
    const examCode = params.get('examCode')
    const promise =
      form && FORMS.includes(form) && examCode
        ? createAttempt({
            form,
            examCode,
            ...pick(params, 'topicId'),
            ...pick(params, 'domainId'),
            ...pick(params, 'scenarioId'),
          } satisfies CreateAttempt)
        : Promise.reject(new ApiError(400, 'This practice link is not valid.'))
    started.current = { key, promise }
    return promise
  }, [key, params])

  const attempt = useLoad(load)
  return (
    <Async state={attempt}>{(a) => <Session key={a.attemptId} attempt={a} />}</Async>
  )
}

function pick(params: URLSearchParams, name: 'topicId' | 'domainId' | 'scenarioId') {
  const value = numberParam(params, name)
  return value === undefined ? {} : { [name]: value }
}

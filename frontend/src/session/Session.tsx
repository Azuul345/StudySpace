import { useRef, useState } from 'react'
import { ApiError, finishAttempt, overrideTyped, submitAnswer } from '../api/client'
import type { Answer, AnswerResult, Attempt, AttemptSummary } from '../api/client'
import { Countdown } from './Countdown'
import { formatCorrect } from './formatCorrect'
import { QuestionView } from './QuestionView'
import { Review } from './Review'

const messageOf = (error: unknown) => (error instanceof ApiError ? error.message : 'Something went wrong.')

/// Runs one attempt. Practice forms give feedback after every answer; the timed exam ('full') does not
/// and ends by early submit, by answering everything and submitting, or by the countdown (REQ-PRAC-001).
export function Session({ attempt }: { attempt: Attempt }) {
  const { questions } = attempt
  const isExam = attempt.form === 'full'
  const [deadline] = useState(() => (attempt.timeLimitSeconds ? Date.now() + attempt.timeLimitSeconds * 1000 : null))

  const [index, setIndex] = useState(0)
  const [result, setResult] = useState<AnswerResult | null>(null)
  const [confirmedCorrect, setConfirmedCorrect] = useState<boolean | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [confirmingSubmit, setConfirmingSubmit] = useState(false)
  const [summary, setSummary] = useState<AttemptSummary | null>(null)
  const [timedOut, setTimedOut] = useState(false)
  const finishing = useRef(false)

  const finish = async (byTimeout = false) => {
    if (finishing.current) return
    finishing.current = true
    setBusy(true)
    setError(null)
    try {
      setSummary(await finishAttempt(attempt.attemptId))
      setTimedOut(byTimeout)
    } catch (e) {
      finishing.current = false
      setError(messageOf(e))
    } finally {
      setBusy(false)
    }
  }

  if (summary) return <Review summary={summary} questions={questions} timedOut={timedOut} />

  const question = questions[index]
  const answeredAll = index >= questions.length

  const answer = async (value: Answer) => {
    setBusy(true)
    setError(null)
    try {
      const graded = await submitAnswer(attempt.attemptId, question.id, value)
      if (isExam) {
        setIndex(index + 1)
      } else {
        setResult(graded)
        setConfirmedCorrect(graded.needsConfirmation ? null : graded.isCorrect)
      }
    } catch (e) {
      setError(messageOf(e))
    } finally {
      setBusy(false)
    }
  }

  const confirm = async (correct: boolean) => {
    setBusy(true)
    setError(null)
    try {
      await overrideTyped(question.id, attempt.attemptId, correct)
      setConfirmedCorrect(correct)
    } catch (e) {
      setError(messageOf(e))
    } finally {
      setBusy(false)
    }
  }

  const next = () => {
    setResult(null)
    setConfirmedCorrect(null)
    setIndex(index + 1)
  }

  const isLast = index === questions.length - 1

  return (
    <section>
      {deadline && <Countdown deadline={deadline} onExpire={() => void finish(true)} />}
      {answeredAll ? (
        <p>You have answered every question. Submit the exam to see your review.</p>
      ) : (
        <>
          <p className="progress">
            Question {index + 1} of {questions.length}
          </p>
          <QuestionView key={question.id} question={question} onSubmit={answer} disabled={busy || result !== null} />
        </>
      )}

      {result && question && (
        <div className="feedback" role="status">
          {result.needsConfirmation && confirmedCorrect === null ? (
            <>
              <p>
                Expected keywords: {formatCorrect(question, result.correct)}. Does your answer cover them?
              </p>
              <button type="button" disabled={busy} onClick={() => void confirm(true)}>
                I got it right
              </button>{' '}
              <button type="button" disabled={busy} onClick={() => void confirm(false)}>
                I got it wrong
              </button>
            </>
          ) : (
            <>
              <p>
                <strong>{confirmedCorrect ? 'Correct' : 'Incorrect'}</strong>
              </p>
              {!confirmedCorrect && !result.needsConfirmation && <p>Answer: {formatCorrect(question, result.correct)}</p>}
              <p>{result.explanation}</p>
              {isLast ? (
                <button type="button" disabled={busy} onClick={() => void finish()}>
                  Finish
                </button>
              ) : (
                <button type="button" onClick={next}>
                  Next question
                </button>
              )}
            </>
          )}
        </div>
      )}

      {error && <p role="alert" className="error">{error}</p>}

      {isExam &&
        (confirmingSubmit ? (
          <p>
            Submit now and see your review?{' '}
            <button type="button" disabled={busy} onClick={() => void finish()}>
              Yes, submit
            </button>{' '}
            <button type="button" onClick={() => setConfirmingSubmit(false)}>
              Keep going
            </button>
          </p>
        ) : (
          <button type="button" onClick={() => setConfirmingSubmit(true)}>
            Submit exam
          </button>
        ))}
    </section>
  )
}

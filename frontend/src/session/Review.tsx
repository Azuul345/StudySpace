import type { AttemptSummary, Question } from '../api/client'
import { Caveat } from '../components/Caveat'
import { formatCorrect } from './formatCorrect'

/// Result of a finished attempt: score, then every question with the right answer and explanation.
export function Review({ summary, questions, timedOut }: { summary: AttemptSummary; questions: Question[]; timedOut?: boolean }) {
  const byId = new Map(questions.map((q) => [q.id, q]))
  return (
    <section>
      <h1>Review</h1>
      {timedOut && <p role="status">Time is up. Your exam was submitted automatically.</p>}
      <p className="score">
        {summary.correct} of {summary.total} correct ({summary.percent}%)
        {summary.answered < summary.total && <small> · {summary.total - summary.answered} unanswered</small>}
      </p>
      <ol className="review">
        {summary.results.map((r) => {
          const question = byId.get(r.questionId)
          return (
            <li key={r.questionId}>
              <p>
                <strong>{r.answered ? (r.isCorrect ? 'Correct' : 'Incorrect') : 'Unanswered'}</strong> · {r.prompt}
              </p>
              {question && !r.isCorrect && <p>Answer: {formatCorrect(question, r.correct)}</p>}
              <p>{r.explanation}</p>
              <Caveat text={r.caveat} />
            </li>
          )
        })}
      </ol>
    </section>
  )
}

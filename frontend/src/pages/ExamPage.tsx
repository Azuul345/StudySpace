import { useCallback } from 'react'
import { useParams } from 'react-router-dom'
import { Link } from 'react-router-dom'
import { getExam } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

export function ExamPage() {
  const { code = '' } = useParams()
  const exam = useLoad(useCallback(() => getExam(code), [code]))
  return (
    <section>
      <Async state={exam}>
        {(e) => (
          <>
            <h1>
              {e.code} <small>{e.name}</small>
            </h1>
            <nav aria-label="Practice" className="actions">
              <Link to={`/practice?form=full&examCode=${encodeURIComponent(e.code)}`}>Start timed exam (65 questions, 130 min)</Link>
              <Link to={`/practice?form=daily&examCode=${encodeURIComponent(e.code)}`}>Daily drill</Link>
              <Link to={`/practice?form=bank&examCode=${encodeURIComponent(e.code)}`}>Wrong-answer review</Link>
              <Link to={`/exams/${encodeURIComponent(e.code)}/flashcards`}>Flashcards</Link>
              <Link to={`/exams/${encodeURIComponent(e.code)}/progress`}>Progress</Link>
              <Link to={`/exams/${encodeURIComponent(e.code)}/bank`}>Wrong-answer bank</Link>
              <Link to={`/exams/${encodeURIComponent(e.code)}/needs-checking`}>Needs checking</Link>
            </nav>
            {e.domains.map((d) => (
              <article key={d.id}>
                <h2>
                  {d.name} <small>{d.weightPercent}% of exam</small>
                </h2>
                <Link to={`/practice?form=drill&examCode=${encodeURIComponent(e.code)}&domainId=${d.id}`}>Drill {d.name}</Link>
                <ul>
                  {d.topics.map((t) => (
                    <li key={t.id}>
                      <Link to={`/topics/${t.id}`}>
                        {t.name} <small>{t.questionCount} questions</small>
                      </Link>
                    </li>
                  ))}
                </ul>
              </article>
            ))}
          </>
        )}
      </Async>
    </section>
  )
}

import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getExam } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

const plural = (n: number) => `${n} ${n === 1 ? 'question' : 'questions'}`

export function ExamPage() {
  const { code = '' } = useParams()
  const exam = useLoad(useCallback(() => getExam(code), [code]))
  return (
    <section>
      <Async state={exam}>
        {(e) => {
          const c = encodeURIComponent(e.code)
          return (
            <>
              <header className="page-head">
                <h1>{e.code}</h1>
                <p className="lede">{e.name}</p>
              </header>
              <nav aria-label="Practice" className="actions">
                <Link className="btn btn-primary" to={`/practice?form=full&examCode=${c}`}>Start timed exam (65 questions, 130 min)</Link>
                <Link className="btn" to={`/practice?form=daily&examCode=${c}`}>Daily drill</Link>
                <Link className="btn" to={`/practice?form=bank&examCode=${c}`}>Wrong-answer review</Link>
                <Link className="btn" to={`/exams/${c}/flashcards`}>Flashcards</Link>
                <Link className="btn" to={`/exams/${c}/progress`}>Progress</Link>
                <Link className="btn" to={`/exams/${c}/bank`}>Wrong-answer bank</Link>
                <Link className="btn" to={`/exams/${c}/needs-checking`}>Needs checking</Link>
              </nav>
              <div className="section-head">
                <h2>Lessons and practice</h2>
                <p className="muted">Open a topic to read its lesson. Use Practice to answer questions on that topic.</p>
              </div>
              {e.domains.map((d) => (
                <article className="card domain" key={d.id}>
                  <div className="domain-head">
                    <h2>{d.name}</h2>
                    <span className="pill">{d.weightPercent}% of exam</span>
                  </div>
                  <div className="weight" aria-hidden="true">
                    <span style={{ width: `${d.weightPercent}%` }} />
                  </div>
                  <ul className="topics">
                    {d.topics.map((t) => (
                      <li className="topic" key={t.id}>
                        <Link className="topic-main" to={`/topics/${t.id}`}>
                          <span className="topic-name">{t.name}</span>
                          <span className="topic-hint">Read the lesson</span>
                        </Link>
                        {t.questionCount > 0 ? (
                          <Link
                            className="btn btn-sm"
                            aria-label={`Practice ${t.name} (${plural(t.questionCount)})`}
                            to={`/practice?form=drill&examCode=${c}&topicId=${t.id}`}
                          >
                            Practice · {plural(t.questionCount)}
                          </Link>
                        ) : (
                          <span className="muted">No questions yet</span>
                        )}
                      </li>
                    ))}
                  </ul>
                  <div>
                    <Link className="btn btn-sm btn-ghost" to={`/practice?form=drill&examCode=${c}&domainId=${d.id}`}>Drill {d.name}</Link>
                  </div>
                </article>
              ))}
            </>
          )
        }}
      </Async>
    </section>
  )
}

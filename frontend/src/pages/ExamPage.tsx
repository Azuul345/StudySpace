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
            {e.domains.map((d) => (
              <article key={d.id}>
                <h2>
                  {d.name} <small>{d.weightPercent}% of exam</small>
                </h2>
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

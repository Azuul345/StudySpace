import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getTopic } from '../api/client'
import { Async } from '../components/Async'
import { Markdown } from '../components/Markdown'
import { Diagram } from '../diagrams/Diagram'
import { diagramRegistry } from '../diagrams/registry'
import { useLoad } from '../components/useLoad'

function httpsOnly(urls: string[]): string[] {
  return urls.filter((u) => {
    try {
      return new URL(u).protocol === 'https:'
    } catch {
      return false
    }
  })
}

export function TopicPage() {
  const { id } = useParams()
  const topic = useLoad(useCallback(() => getTopic(Number(id)), [id]))
  return (
    <section>
      <Async state={topic}>
        {(t) => {
          const links = t.lesson ? httpsOnly(t.lesson.docLinks) : []
          const diagrams = (t.lesson?.diagrams ?? []).flatMap((d) => (diagramRegistry[d] ? [{ id: d, def: diagramRegistry[d] }] : []))
          return (
            <>
              <header className="page-head">
                <p className="crumb">
                  <Link to={`/exams/${encodeURIComponent(t.examCode)}`}>{t.examCode}</Link> / {t.domain.name}
                </p>
                <div className="title-row">
                  <h1>{t.name}</h1>
                  <span className="pill">Lesson</span>
                </div>
              </header>
              {t.questionCount > 0 && (
                <div className="card cta">
                  <p>
                    <strong>Done reading?</strong> <span className="muted">Try {t.questionCount} practice questions on this topic.</span>
                  </p>
                  <Link className="btn btn-primary" to={`/practice?form=drill&examCode=${encodeURIComponent(t.examCode)}&topicId=${t.id}`}>
                    Practice this topic
                  </Link>
                </div>
              )}
              {t.lesson ? (
                <div className="lesson">
                  <Markdown source={t.lesson.summary} />
                  <h2>Key points</h2>
                  <ul className="keypoints">
                    {t.lesson.keyPoints.map((k) => (
                      <li key={k}>
                        <Markdown source={k} />
                      </li>
                    ))}
                  </ul>
                  {diagrams.map(({ id: key, def }) => (
                    <Diagram key={key} def={def} />
                  ))}
                  {links.length > 0 && (
                    <>
                      <h2>Documentation</h2>
                      <ul>
                        {links.map((u) => (
                          <li key={u}>
                            <a href={u} target="_blank" rel="noopener noreferrer">
                              {u}
                            </a>
                          </li>
                        ))}
                      </ul>
                    </>
                  )}
                </div>
              ) : (
                <p>No lesson for this topic yet.</p>
              )}
            </>
          )
        }}
      </Async>
    </section>
  )
}
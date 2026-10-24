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
        {(t) => (
          <>
            <p className="crumb">{t.domain.name}</p>
            <h1>{t.name}</h1>
            {t.lesson ? (
              <>
                <Markdown source={t.lesson.summary} />
                <h2>Key points</h2>
                <ul>
                  {t.lesson.keyPoints.map((k) => (
                    <li key={k}>
                      <Markdown source={k} />
                    </li>
                  ))}
                </ul>
                {(t.lesson.diagrams ?? []).flatMap((id) => (diagramRegistry[id] ? [{ id, def: diagramRegistry[id] }] : [])).map(({ id, def }) => (
                  <Diagram key={id} def={def} />
                ))}
                {httpsOnly(t.lesson.docLinks).length > 0 && (
                  <>
                    <h2>Documentation</h2>
                    <ul>
                      {httpsOnly(t.lesson.docLinks).map((u) => (
                        <li key={u}>
                          <a href={u} target="_blank" rel="noopener noreferrer">
                            {u}
                          </a>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </>
            ) : (
              <p>No lesson for this topic yet.</p>
            )}
            <p>{t.questionCount} verified questions available.</p>
            {t.questionCount > 0 && (
              <Link to={`/practice?form=drill&examCode=${encodeURIComponent(t.examCode)}&topicId=${t.id}`}>Start drill</Link>
            )}
          </>
        )}
      </Async>
    </section>
  )
}

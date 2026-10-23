import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getProgress } from '../api/client'
import type { MasteryLevel, TopicProgress } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

const LABELS: Record<MasteryLevel, string> = { unseen: 'Not started', weak: 'Weak', strong: 'Strong' }
const percent = (t: TopicProgress) => (t.answered === 0 ? '–' : `${Math.round(t.accuracy * 100)}%`)

export function ProgressPage() {
  const { code = '' } = useParams()
  const progress = useLoad(useCallback(() => getProgress(code), [code]))
  return (
    <section>
      <h1>Progress</h1>
      <Async state={progress}>
        {(p) =>
          p.domains.map((d) => (
            <article key={d.id}>
              <h2>
                {d.name} <small>{percent(d)} · {LABELS[d.level]} · {d.activeBankCount} to review</small>
              </h2>
              <table>
                <thead>
                  <tr>
                    <th>Topic</th>
                    <th>Level</th>
                    <th>Accuracy</th>
                    <th>Correct</th>
                    <th>In bank</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {d.topics.map((t) => (
                    <tr key={t.id}>
                      <td>{t.name}</td>
                      <td className={`level ${t.level}`}>{LABELS[t.level]}</td>
                      <td>{percent(t)}</td>
                      <td>{t.answered === 0 ? '–' : `${t.correct} / ${t.answered}`}</td>
                      <td>{t.activeBankCount}</td>
                      <td>
                        <Link
                          aria-label={`Drill ${t.name}`}
                          to={`/practice?form=drill&examCode=${encodeURIComponent(code)}&topicId=${t.id}`}
                        >
                          Drill
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
          ))
        }
      </Async>
    </section>
  )
}

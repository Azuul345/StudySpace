import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getNeedsChecking } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

const isHttps = (url: string) => url.startsWith('https://')

export function NeedsCheckingPage() {
  const { code = '' } = useParams()
  const list = useLoad(useCallback(() => getNeedsChecking(code), [code]))
  return (
    <section>
      <h1>Needs checking</h1>
      <p>
        Every question was checked by a separate verifier agent against official AWS pages, but an agent is not a person. These questions
        passed on their main claim, yet part of the explanation could not be confirmed. Look them up and send the question reference with a
        source (or a correction) so the content can be updated.
      </p>
      <Async state={list}>
        {(l) =>
          l.total === 0 ? (
            <p>Nothing is flagged right now.</p>
          ) : (
            <>
              <p>
                {l.total} {l.total === 1 ? 'question' : 'questions'} to double check
              </p>
              <ol className="check-list">
                {l.items.map((i) => (
                  <li key={i.questionId}>
                    <p>
                      <code>{i.reference}</code> · <small>{i.domainName} · </small>
                      <Link to={`/topics/${i.topicId}`}>{i.topicName}</Link>
                    </p>
                    <p>{i.prompt}</p>
                    <p>
                      <strong>Marked answer:</strong> {i.answer}
                    </p>
                    <p className="caveat" role="note">
                      <strong>Could not confirm:</strong> {i.caveat}
                    </p>
                    <p>{i.explanation}</p>
                    {i.sources.length > 0 && (
                      <ul>
                        {i.sources.map((s) => (
                          <li key={s}>
                            {isHttps(s) ? (
                              <a href={s} target="_blank" rel="noopener noreferrer">
                                {s}
                              </a>
                            ) : (
                              s
                            )}
                          </li>
                        ))}
                      </ul>
                    )}
                  </li>
                ))}
              </ol>
            </>
          )
        }
      </Async>
    </section>
  )
}

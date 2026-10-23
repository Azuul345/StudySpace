import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getBank } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

// Mirrors the server's graduation rule (REQ-BANK-001).
const STREAK_TO_MASTER = 5

export function BankPage() {
  const { code = '' } = useParams()
  const bank = useLoad(useCallback(() => getBank(code), [code]))
  return (
    <section>
      <h1>Wrong-answer bank</h1>
      <Async state={bank}>
        {(b) =>
          b.total === 0 ? (
            <p>Nothing to review. Questions you get wrong will show up here.</p>
          ) : (
            <>
              <p>
                {b.total} {b.total === 1 ? 'question' : 'questions'} to review
              </p>
              <Link to={`/practice?form=bank&examCode=${encodeURIComponent(code)}`}>Review these</Link>
              {b.categories.map((c) => (
                <article key={c.domainId}>
                  <h2>{c.domainName}</h2>
                  <ul>
                    {c.items.map((i) => (
                      <li key={i.questionId}>
                        <p>{i.prompt}</p>
                        <small>
                          {i.topicName} · {i.consecutiveCorrect} of {STREAK_TO_MASTER} correct in a row
                        </small>
                      </li>
                    ))}
                  </ul>
                </article>
              ))}
            </>
          )
        }
      </Async>
    </section>
  )
}

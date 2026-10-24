import { Link } from 'react-router-dom'
import { getExams } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

export function Dashboard() {
  const exams = useLoad(getExams)
  return (
    <section>
      <header className="page-head">
        <h1>Dashboard</h1>
        <p className="lede">Pick an exam to read lessons and practice.</p>
      </header>
      <Async state={exams}>
        {(list) =>
          list.length === 0 ? (
            <p>No exams yet. Import a content pack with the import tool.</p>
          ) : (
            <ul className="cards">
              {list.map((e) => (
                <li key={e.code}>
                  <Link className="card exam-card" to={`/exams/${encodeURIComponent(e.code)}`}>
                    <h2>
                      {e.code} <span className="pill">v{e.version}</span>
                    </h2>
                    <span className="muted">{e.name}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )
        }
      </Async>
    </section>
  )
}

import { Link } from 'react-router-dom'
import { getExams } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'

export function Dashboard() {
  const exams = useLoad(getExams)
  return (
    <section>
      <h1>Dashboard</h1>
      <Async state={exams}>
        {(list) =>
          list.length === 0 ? (
            <p>No exams yet. Import a content pack with the import tool.</p>
          ) : (
            <ul className="cards">
              {list.map((e) => (
                <li key={e.code}>
                  <Link to={`/exams/${encodeURIComponent(e.code)}`}>
                    <strong>{e.code}</strong> {e.name}
                  </Link>
                  <small> v{e.version}</small>
                </li>
              ))}
            </ul>
          )
        }
      </Async>
    </section>
  )
}

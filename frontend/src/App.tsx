import { Link, NavLink, Route, Routes } from 'react-router-dom'
import { Dashboard } from './pages/Dashboard'
import { ExamPage } from './pages/ExamPage'
import { TopicPage } from './pages/TopicPage'
import './App.css'

function NotFound() {
  return (
    <section>
      <h1>Page not found</h1>
      <Link to="/">Back to the dashboard</Link>
    </section>
  )
}

export function App() {
  return (
    <>
      <nav aria-label="Main">
        <NavLink to="/" end>
          Dashboard
        </NavLink>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/exams/:code" element={<ExamPage />} />
          <Route path="/topics/:id" element={<TopicPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </>
  )
}

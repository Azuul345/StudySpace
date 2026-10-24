import { Link, NavLink, Route, Routes } from 'react-router-dom'
import { Dashboard } from './pages/Dashboard'
import { ExamPage } from './pages/ExamPage'
import { TopicPage } from './pages/TopicPage'
import { FlashcardsPage } from './pages/FlashcardsPage'
import { PracticePage } from './pages/PracticePage'
import { ProgressPage } from './pages/ProgressPage'
import { BankPage } from './pages/BankPage'
import { NeedsCheckingPage } from './pages/NeedsCheckingPage'
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
          <Route path="/exams/:code/flashcards" element={<FlashcardsPage />} />
          <Route path="/exams/:code/progress" element={<ProgressPage />} />
          <Route path="/exams/:code/bank" element={<BankPage />} />
          <Route path="/exams/:code/needs-checking" element={<NeedsCheckingPage />} />
          <Route path="/practice" element={<PracticePage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </>
  )
}

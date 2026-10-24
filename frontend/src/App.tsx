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
      <nav aria-label="Main" className="topbar">
        <Link to="/" className="brand">
          <svg width="26" height="26" viewBox="0 0 24 24" aria-hidden="true">
            <rect width="24" height="24" rx="7" fill="var(--primary)" />
            <path d="M5.5 8.2c2.3-.9 4.4-.7 6.5.7 2.1-1.4 4.2-1.6 6.5-.7v8.3c-2.3-.9-4.4-.7-6.5.7-2.1-1.4-4.2-1.6-6.5-.7z" fill="none" stroke="var(--primary-ink)" strokeWidth="1.7" strokeLinejoin="round" />
            <path d="M12 8.9v8.3" stroke="var(--primary-ink)" strokeWidth="1.7" />
          </svg>
          StudySpace
        </Link>
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

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export interface ExamSummary {
  code: string
  name: string
  version: string
}

export interface TopicSummary {
  id: number
  slug: string
  name: string
  questionCount: number
}

export interface DomainDetail {
  id: number
  slug: string
  name: string
  weightPercent: number
  topics: TopicSummary[]
}

export interface ExamDetail extends ExamSummary {
  domains: DomainDetail[]
}

export interface Lesson {
  summary: string
  keyPoints: string[]
  docLinks: string[]
}

export interface TopicDetail {
  id: number
  slug: string
  name: string
  examCode: string
  domain: { id: number; name: string }
  lesson: Lesson | null
  questionCount: number
}

const BASE = '/api/v1'

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let title = `Request failed (${response.status}).`
    try {
      const problem = (await response.json()) as { title?: string }
      if (problem.title) title = problem.title
    } catch {
      // keep the generic message
    }
    throw new ApiError(response.status, title)
  }
  return (await response.json()) as T
}

async function get<T>(path: string): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE}${path}`, { headers: { Accept: 'application/json' } })
  } catch {
    throw new ApiError(0, 'Cannot reach the API. Is the backend running?')
  }
  return parse<T>(response)
}

export const getExams = () => get<ExamSummary[]>('/exams')
export const getExam = (code: string) => get<ExamDetail>(`/exams/${encodeURIComponent(code)}`)
export const getTopic = (id: number) => get<TopicDetail>(`/topics/${id}`)

// ---- Practice (P5) ----

export type QuestionType = 'mcq' | 'multi' | 'scenario' | 'matching' | 'truefalse' | 'ordering' | 'typed'

export interface Question {
  id: number
  type: QuestionType
  prompt: string
  difficulty: number
  tags: string[]
  topicId: number
  scenario: { id: number; text: string } | null
  options: { id: string; text: string }[] | null
  pairs: { lefts: string[]; rights: string[] } | null
  steps: string[] | null
}

/// The answer shapes the server grades: selected / value / order / pairs / text.
export type Answer =
  | { selected: string[] }
  | { value: boolean }
  | { order: number[] }
  | { pairs: { left: string; right: string }[] }
  | { text: string }

export type PracticeForm = 'full' | 'drill' | 'daily' | 'bank' | 'scenario'

export interface CreateAttempt {
  examCode: string
  form: PracticeForm
  topicId?: number
  domainId?: number
  scenarioId?: number
  count?: number
}

export interface Attempt {
  attemptId: number
  form: PracticeForm
  startedAt: string
  timeLimitSeconds: number | null
  questions: Question[]
}

export interface AnswerResult {
  questionId: number
  isCorrect: boolean
  needsConfirmation: boolean
  explanation: string
  caveat?: string | null
  correct: unknown
}

export interface QuestionOutcome {
  questionId: number
  prompt: string
  answered: boolean
  isCorrect: boolean | null
  given: unknown
  explanation: string
  caveat?: string | null
  correct: unknown
}

export interface AttemptSummary {
  attemptId: number
  form: PracticeForm
  total: number
  answered: number
  correct: number
  percent: number
  results: QuestionOutcome[]
}

export interface Flashcard {
  id: number
  term: string
  definition: string
  isNew: boolean
}

async function post<T>(path: string, body: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Cannot reach the API. Is the backend running?')
  }
  return parse<T>(response)
}

export const createAttempt = (request: CreateAttempt) => post<Attempt>('/attempts', request)
export const submitAnswer = (attemptId: number, questionId: number, answer: Answer) =>
  post<AnswerResult>(`/attempts/${attemptId}/answers`, { questionId, answer })
export const finishAttempt = (attemptId: number) => post<AttemptSummary>(`/attempts/${attemptId}/finish`, {})
export const overrideTyped = (questionId: number, attemptId: number, correct: boolean) =>
  post<{ questionId: number; isCorrect: boolean }>(`/typed/${questionId}/override`, { attemptId, correct })
export const getDueFlashcards = (examCode: string, limit = 20) =>
  get<Flashcard[]>(`/flashcards/due?examCode=${encodeURIComponent(examCode)}&limit=${limit}`)
export const reviewFlashcard = (id: number, quality: number) => post<unknown>(`/flashcards/${id}/review`, { quality })

// ---- Progress and bank (P6) ----

export type MasteryLevel = 'unseen' | 'weak' | 'strong'

export interface TopicProgress {
  id: number
  name: string
  answered: number
  correct: number
  accuracy: number
  score: number
  level: MasteryLevel
  activeBankCount: number
}

export interface DomainProgress extends TopicProgress {
  weightPercent: number
  topics: TopicProgress[]
}

export interface Progress {
  code: string
  domains: DomainProgress[]
}

export interface BankItem {
  questionId: number
  topicId: number
  topicName: string
  prompt: string
  consecutiveCorrect: number
  lastWrongAt: string | null
}

export interface Bank {
  total: number
  categories: { domainId: number; domainName: string; items: BankItem[] }[]
}

export const getProgress = (examCode: string) => get<Progress>(`/progress?examCode=${encodeURIComponent(examCode)}`)
export const getBank = (examCode: string) => get<Bank>(`/bank?examCode=${encodeURIComponent(examCode)}`)

// ---- Needs checking ----
export interface NeedsCheckingItem {
  questionId: number
  reference: string
  type: string
  prompt: string
  topicId: number
  topicName: string
  domainName: string
  caveat: string
  answer: string
  explanation: string
  sources: string[]
}

export interface NeedsChecking {
  total: number
  items: NeedsCheckingItem[]
}

export const getNeedsChecking = (examCode: string) => get<NeedsChecking>(`/needs-checking?examCode=${encodeURIComponent(examCode)}`)

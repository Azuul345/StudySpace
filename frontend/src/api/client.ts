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
  domain: { id: number; name: string }
  lesson: Lesson | null
  questionCount: number
}

const BASE = '/api/v1'

async function get<T>(path: string): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE}${path}`, { headers: { Accept: 'application/json' } })
  } catch {
    throw new ApiError(0, 'Cannot reach the API. Is the backend running?')
  }

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

export const getExams = () => get<ExamSummary[]>('/exams')
export const getExam = (code: string) => get<ExamDetail>(`/exams/${encodeURIComponent(code)}`)
export const getTopic = (id: number) => get<TopicDetail>(`/topics/${id}`)

import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'
import * as client from '../api/client'

vi.mock('../api/client', async (orig) => ({
  ...(await orig<typeof client>()),
  getExam: vi.fn(), getTopic: vi.fn(), createAttempt: vi.fn(), getDueFlashcards: vi.fn(), reviewFlashcard: vi.fn(),
}))

const exam: client.ExamDetail = {
  code: 'SAA-C03', name: 'AWS SAA', version: '1',
  domains: [{ id: 4, slug: 'secure', name: 'Secure', weightPercent: 30, topics: [{ id: 7, slug: 's3', name: 'S3', questionCount: 3 }] }],
}

const attempt: client.Attempt = {
  attemptId: 1, form: 'drill', startedAt: '2026-10-23T12:00:00Z', timeLimitSeconds: null,
  questions: [{
    id: 1, type: 'mcq', prompt: 'Which?', difficulty: 1, tags: [], topicId: 7,
    scenario: null, options: [{ id: 'a', text: 'Alpha' }, { id: 'b', text: 'Beta' }], pairs: null, steps: null,
  }],
}

const renderAt = (path: string) =>
  render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>)

beforeEach(() => {
  vi.mocked(client.getExam).mockResolvedValue(exam)
  vi.mocked(client.createAttempt).mockResolvedValue(attempt)
  vi.mocked(client.getTopic).mockResolvedValue({
    id: 7, slug: 's3', name: 'S3', examCode: 'SAA-C03', domain: { id: 4, name: 'Secure' }, lesson: null, questionCount: 3,
  })
})

describe('start links', () => {
  it('exam page offers every practice form', async () => {
    renderAt('/exams/SAA-C03')
    expect(await screen.findByRole('link', { name: /timed exam/i })).toHaveAttribute('href', '/practice?form=full&examCode=SAA-C03')
    expect(screen.getByRole('link', { name: /daily drill/i })).toHaveAttribute('href', '/practice?form=daily&examCode=SAA-C03')
    expect(screen.getByRole('link', { name: /wrong-answer review/i })).toHaveAttribute('href', '/practice?form=bank&examCode=SAA-C03')
    expect(screen.getByRole('link', { name: /flashcards/i })).toHaveAttribute('href', '/exams/SAA-C03/flashcards')
    expect(screen.getByRole('link', { name: /drill secure/i })).toHaveAttribute('href', '/practice?form=drill&examCode=SAA-C03&domainId=4')
  })

  it('topic page starts a topic drill', async () => {
    renderAt('/topics/7')
    expect(await screen.findByRole('link', { name: /practice this topic/i })).toHaveAttribute(
      'href', '/practice?form=drill&examCode=SAA-C03&topicId=7')
  })
})

describe('practice page', () => {
  it('creates the attempt from the query and shows the first question', async () => {
    renderAt('/practice?form=drill&examCode=SAA-C03&topicId=7')
    expect(await screen.findByText('Which?')).toBeInTheDocument()
    expect(client.createAttempt).toHaveBeenCalledTimes(1)
    expect(client.createAttempt).toHaveBeenCalledWith({ form: 'drill', examCode: 'SAA-C03', topicId: 7 })
  })

  it('rejects an unknown form without calling the API', async () => {
    renderAt('/practice?form=bogus&examCode=SAA-C03')
    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(client.createAttempt).not.toHaveBeenCalled()
  })

  it('shows the server message when nothing is available', async () => {
    vi.mocked(client.createAttempt).mockRejectedValue(new client.ApiError(409, 'No verified questions are available for this practice form.'))
    renderAt('/practice?form=bank&examCode=SAA-C03')
    expect(await screen.findByRole('alert')).toHaveTextContent('No verified questions')
  })
})

describe('flashcards page', () => {
  const cards: client.Flashcard[] = [
    { id: 1, term: 'Bucket', definition: 'A container', isNew: true },
    { id: 2, term: 'Region', definition: 'A location', isNew: false },
  ]
  it('rates cards one at a time and ends with a done message', async () => {
    vi.mocked(client.getDueFlashcards).mockResolvedValue(cards)
    vi.mocked(client.reviewFlashcard).mockResolvedValue({})
    renderAt('/exams/SAA-C03/flashcards')
    expect(await screen.findByText('Bucket')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /show answer/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Good' }))
    expect(client.reviewFlashcard).toHaveBeenCalledWith(1, 4)
    expect(await screen.findByText('Region')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /show answer/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Again' }))
    expect(await screen.findByText(/all done/i)).toBeInTheDocument()
  })

  it('shows an empty state when nothing is due', async () => {
    vi.mocked(client.getDueFlashcards).mockResolvedValue([])
    renderAt('/exams/SAA-C03/flashcards')
    expect(await screen.findByText(/no flashcards are due/i)).toBeInTheDocument()
  })
})

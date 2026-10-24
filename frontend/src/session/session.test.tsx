import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as client from '../api/client'
import { Session } from './Session'

vi.mock('../api/client', async (orig) => ({
  ...(await orig<typeof client>()),
  submitAnswer: vi.fn(),
  finishAttempt: vi.fn(),
  overrideTyped: vi.fn(),
}))

const q = (id: number, type: client.QuestionType, extra: Partial<client.Question> = {}): client.Question => ({
  id, type, prompt: `Prompt ${id}`, difficulty: 1, tags: [], topicId: 1,
  scenario: null, options: null, pairs: null, steps: null, ...extra,
})

const mcq = q(1, 'mcq', { options: [{ id: 'a', text: 'Alpha' }, { id: 'b', text: 'Beta' }] })
const typed = q(2, 'typed')

const attempt = (form: client.PracticeForm, questions: client.Question[], timeLimitSeconds: number | null = null): client.Attempt => ({
  attemptId: 10, form, startedAt: '2026-10-23T12:00:00Z', timeLimitSeconds, questions,
})

const summary: client.AttemptSummary = {
  attemptId: 10, form: 'drill', total: 2, answered: 2, correct: 1, percent: 50,
  results: [
    { questionId: 1, prompt: 'Prompt 1', answered: true, isCorrect: true, given: {}, explanation: 'Because A.', correct: {} },
    { questionId: 2, prompt: 'Prompt 2', answered: true, isCorrect: false, given: {}, explanation: 'Because S3.', correct: {} },
  ],
}

beforeEach(() => {
  vi.mocked(client.finishAttempt).mockResolvedValue(summary)
  vi.mocked(client.submitAnswer).mockResolvedValue({
    questionId: 1, isCorrect: true, needsConfirmation: false, explanation: 'Alpha is right.', correct: { selected: ['a'] },
  })
})
afterEach(() => vi.clearAllMocks())

describe('practice session', () => {
  it('grades on the server and shows feedback after each answer', async () => {
    render(<Session attempt={attempt('drill', [mcq, typed])} />)
    expect(screen.getByText(/question 1 of 2/i)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(client.submitAnswer).toHaveBeenCalledWith(10, 1, { selected: ['a'] })
    expect(await screen.findByText('Correct')).toBeInTheDocument()
    expect(screen.getByText('Alpha is right.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /next question/i }))
    expect(screen.getByText(/question 2 of 2/i)).toBeInTheDocument()
  })

  it('discloses a caveat in feedback only when the question has one', async () => {
    vi.mocked(client.submitAnswer).mockResolvedValue({
      questionId: 1, isCorrect: true, needsConfirmation: false, explanation: 'Alpha is right.', caveat: 'Limit not on the page.', correct: { selected: ['a'] },
    })
    render(<Session attempt={attempt('drill', [mcq])} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByText(/not fully verified/i)).toBeInTheDocument()
    expect(screen.getByText(/limit not on the page/i)).toBeInTheDocument()
  })

  it('shows no caveat note for a fully verified question', async () => {
    render(<Session attempt={attempt('drill', [mcq])} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    await screen.findByText('Correct')
    expect(screen.queryByText(/not fully verified/i)).toBeNull()
  })

  it('shows Incorrect feedback', async () => {
    vi.mocked(client.submitAnswer).mockResolvedValue({
      questionId: 1, isCorrect: false, needsConfirmation: false, explanation: 'No.', correct: { selected: ['b'] },
    })
    render(<Session attempt={attempt('drill', [mcq])} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByText('Incorrect')).toBeInTheDocument()
  })

  it('asks the user to confirm a typed answer and stores the override', async () => {
    vi.mocked(client.submitAnswer).mockResolvedValue({
      questionId: 2, isCorrect: false, needsConfirmation: true, explanation: 'S3 stores objects.',
      correct: { keywords: [['s3', 'simple storage service']] },
    })
    vi.mocked(client.overrideTyped).mockResolvedValue({ questionId: 2, isCorrect: true })
    render(<Session attempt={attempt('drill', [typed])} />)
    await userEvent.type(screen.getByRole('textbox'), 'object store')
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByText(/s3, simple storage service/i)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /i got it right/i }))
    expect(client.overrideTyped).toHaveBeenCalledWith(2, 10, true)
    expect(await screen.findByText('Correct')).toBeInTheDocument()
  })

  it('finishes after the last question and shows the review', async () => {
    render(<Session attempt={attempt('drill', [mcq])} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    await userEvent.click(await screen.findByRole('button', { name: /finish/i }))
    expect(client.finishAttempt).toHaveBeenCalledWith(10)
    expect(await screen.findByText(/1 of 2 correct/i)).toBeInTheDocument()
    expect(screen.getByText('Because S3.')).toBeInTheDocument()
  })

  it('shows an error when the answer cannot be saved and allows retry', async () => {
    vi.mocked(client.submitAnswer).mockRejectedValueOnce(new client.ApiError(0, 'Cannot reach the API.'))
    render(<Session attempt={attempt('drill', [mcq])} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the API.')
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByText('Correct')).toBeInTheDocument()
  })
})

describe('timed exam (REQ-PRAC-001)', () => {
  it('hides feedback while the exam is running and moves on', async () => {
    render(<Session attempt={attempt('full', [mcq, typed], 7800)} />)
    expect(screen.getByRole('timer')).toHaveTextContent('2:10:00')
    await userEvent.click(screen.getByRole('radio', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('button', { name: /submit answer/i }))
    expect(await screen.findByText(/question 2 of 2/i)).toBeInTheDocument()
    expect(screen.queryByText('Correct')).toBeNull()
    expect(screen.queryByText('Alpha is right.')).toBeNull()
  })

  it('submits early and goes to the review screen', async () => {
    render(<Session attempt={attempt('full', [mcq, typed], 7800)} />)
    await userEvent.click(screen.getByRole('button', { name: /submit exam/i }))
    await userEvent.click(screen.getByRole('button', { name: /yes, submit/i }))
    expect(client.finishAttempt).toHaveBeenCalledWith(10)
    expect(await screen.findByText(/1 of 2 correct/i)).toBeInTheDocument()
  })

  it('auto-submits at 130 minutes (fake clock)', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    try {
      render(<Session attempt={attempt('full', [mcq, typed], 7800)} />)
      expect(client.finishAttempt).not.toHaveBeenCalled()
      await act(async () => {
        vi.advanceTimersByTime(7800 * 1000 - 1000)
      })
      expect(client.finishAttempt).not.toHaveBeenCalled()
      await act(async () => {
        vi.advanceTimersByTime(1500)
      })
      await waitFor(() => expect(client.finishAttempt).toHaveBeenCalledTimes(1))
      expect(await screen.findByText(/time is up/i)).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })
})

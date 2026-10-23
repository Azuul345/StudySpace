import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'
import * as client from '../api/client'

vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof client>()), getProgress: vi.fn(), getBank: vi.fn(), getExam: vi.fn() }))

const topic = (id: number, name: string, level: client.MasteryLevel, answered: number, correct: number, bank: number): client.TopicProgress => ({
  id, name, answered, correct, accuracy: answered ? correct / answered : 0, score: 0.5, level, activeBankCount: bank,
})

const progress: client.Progress = {
  code: 'SAA-C03',
  domains: [
    {
      ...topic(1, 'Secure', 'weak', 10, 6, 3), weightPercent: 30,
      topics: [topic(7, 'S3 security', 'weak', 6, 2, 3), topic(8, 'IAM', 'strong', 4, 4, 0), topic(9, 'KMS', 'unseen', 0, 0, 0)],
    },
  ],
}

const bank: client.Bank = {
  total: 2,
  categories: [{
    domainId: 1, domainName: 'Secure',
    items: [
      { questionId: 11, topicId: 7, topicName: 'S3 security', prompt: 'Which blocks public access?', consecutiveCorrect: 3, lastWrongAt: '2026-10-20T10:00:00Z' },
      { questionId: 12, topicId: 7, topicName: 'S3 security', prompt: 'Which encrypts at rest?', consecutiveCorrect: 0, lastWrongAt: '2026-10-22T10:00:00Z' },
    ],
  }],
}

const renderAt = (path: string) => render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>)

beforeEach(() => {
  vi.mocked(client.getProgress).mockResolvedValue(progress)
  vi.mocked(client.getBank).mockResolvedValue(bank)
})

describe('Progress page (REQ-PROG-001, REQ-PROG-003)', () => {
  it('shows accuracy and bank count per domain and topic', async () => {
    renderAt('/exams/SAA-C03/progress')
    const domain = await screen.findByRole('heading', { name: /Secure/ })
    expect(domain).toHaveTextContent('60%')
    const row = screen.getByRole('row', { name: /S3 security/ })
    expect(within(row).getByText('33%')).toBeInTheDocument()
    expect(within(row).getByText('2 / 6')).toBeInTheDocument()
    expect(within(row).getByText('3')).toBeInTheDocument()
  })

  it('labels topics weak, strong or unseen', async () => {
    renderAt('/exams/SAA-C03/progress')
    await screen.findByRole('heading', { name: /Secure/ })
    expect(within(screen.getByRole('row', { name: /S3 security/ })).getByText('Weak')).toBeInTheDocument()
    expect(within(screen.getByRole('row', { name: /IAM/ })).getByText('Strong')).toBeInTheDocument()
    expect(within(screen.getByRole('row', { name: /KMS/ })).getByText('Not started')).toBeInTheDocument()
    expect(within(screen.getByRole('row', { name: /KMS/ })).getAllByText('–')).toHaveLength(2)
  })

  it('links each topic to a drill', async () => {
    renderAt('/exams/SAA-C03/progress')
    const link = await screen.findByRole('link', { name: 'Drill S3 security' })
    expect(link).toHaveAttribute('href', '/practice?form=drill&examCode=SAA-C03&topicId=7')
  })

  it('shows an error from the API', async () => {
    vi.mocked(client.getProgress).mockRejectedValue(new client.ApiError(404, 'Exam not found.'))
    renderAt('/exams/NOPE/progress')
    expect(await screen.findByRole('alert')).toHaveTextContent('Exam not found.')
  })
})

describe('Wrong-answer bank page (REQ-BANK-001)', () => {
  it('groups active items by domain with streaks toward mastery', async () => {
    renderAt('/exams/SAA-C03/bank')
    expect(await screen.findByRole('heading', { name: /Secure/ })).toBeInTheDocument()
    expect(screen.getByText('2 questions to review')).toBeInTheDocument()
    expect(screen.getByText('Which blocks public access?')).toBeInTheDocument()
    expect(screen.getByText(/3 of 5 correct in a row/)).toBeInTheDocument()
    expect(screen.getByText(/0 of 5 correct in a row/)).toBeInTheDocument()
  })

  it('starts a bank review', async () => {
    renderAt('/exams/SAA-C03/bank')
    expect(await screen.findByRole('link', { name: /review these/i })).toHaveAttribute('href', '/practice?form=bank&examCode=SAA-C03')
  })

  it('shows an empty state with no link when the bank is empty', async () => {
    vi.mocked(client.getBank).mockResolvedValue({ total: 0, categories: [] })
    renderAt('/exams/SAA-C03/bank')
    expect(await screen.findByText(/nothing to review/i)).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /review these/i })).toBeNull()
  })
})

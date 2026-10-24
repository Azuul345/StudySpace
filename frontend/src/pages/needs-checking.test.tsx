import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'
import * as client from '../api/client'

vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof client>()), getNeedsChecking: vi.fn(), getExam: vi.fn() }))

const item = (extra: Partial<client.NeedsCheckingItem> = {}): client.NeedsCheckingItem => ({
  questionId: 4, reference: 'cost-network-002', type: 'mcq', prompt: 'Which reduces transfer cost?', topicId: 9, topicName: 'Network cost',
  domainName: 'Cost', caveat: 'The fee claim is not on the cited page.', answer: 'b) Use CloudFront', explanation: 'CloudFront caches.',
  sources: ['https://docs.aws.amazon.com/x', 'javascript:alert(1)'], ...extra,
})

const renderAt = (path: string) => render(<MemoryRouter initialEntries={[path]}><App /></MemoryRouter>)

beforeEach(() => vi.clearAllMocks())

describe('Needs checking page', () => {
  it('lists each flagged question with its reference, answer, caveat and sources', async () => {
    vi.mocked(client.getNeedsChecking).mockResolvedValue({ total: 1, items: [item()] })
    renderAt('/exams/SAA-C03/needs-checking')

    expect(await screen.findByText('cost-network-002')).toBeInTheDocument()
    expect(client.getNeedsChecking).toHaveBeenCalledWith('SAA-C03')
    expect(screen.getByText('Which reduces transfer cost?')).toBeInTheDocument()
    expect(screen.getByText(/b\) Use CloudFront/)).toBeInTheDocument()
    expect(screen.getByText(/fee claim is not on the cited page/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Network cost' })).toHaveAttribute('href', '/topics/9')
  })

  it('links only https sources, with safe rel', async () => {
    vi.mocked(client.getNeedsChecking).mockResolvedValue({ total: 1, items: [item()] })
    renderAt('/exams/SAA-C03/needs-checking')

    const link = await screen.findByRole('link', { name: 'https://docs.aws.amazon.com/x' })
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    expect(screen.queryByRole('link', { name: /javascript:/ })).toBeNull()
    expect(screen.getByText('javascript:alert(1)')).toBeInTheDocument()
  })

  it('says so when nothing is flagged', async () => {
    vi.mocked(client.getNeedsChecking).mockResolvedValue({ total: 0, items: [] })
    renderAt('/exams/SAA-C03/needs-checking')

    expect(await screen.findByText(/nothing is flagged/i)).toBeInTheDocument()
  })

  it('is reachable from the exam page', async () => {
    vi.mocked(client.getExam).mockResolvedValue({ code: 'SAA-C03', name: 'AWS SAA', domains: [] } as unknown as client.ExamDetail)
    renderAt('/exams/SAA-C03')

    expect(await screen.findByRole('link', { name: /needs checking/i })).toHaveAttribute('href', '/exams/SAA-C03/needs-checking')
  })
})

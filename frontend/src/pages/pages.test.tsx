import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'
import * as client from '../api/client'

vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof client>()), getExams: vi.fn(), getExam: vi.fn(), getTopic: vi.fn() }))

const exam: client.ExamDetail = {
  code: 'SAA-C03',
  name: 'AWS Certified Solutions Architect – Associate',
  version: '2026.10.1',
  domains: [
    {
      id: 1,
      slug: 'secure',
      name: 'Design Secure Architectures',
      weightPercent: 30,
      topics: [{ id: 7, slug: 's3-security', name: 'S3 security and access', questionCount: 12 }],
    },
  ],
}

const topic: client.TopicDetail = {
  id: 7,
  slug: 's3-security',
  name: 'S3 security and access',
  domain: { id: 1, name: 'Design Secure Architectures' },
  lesson: {
    summary: 'Use **bucket policies** with Block Public Access.',
    keyPoints: ['Bucket policies vs IAM policies', '<script>alert(1)</script>'],
    docLinks: ['https://docs.aws.amazon.com/AmazonS3/latest/userguide/x.html', 'javascript:alert(1)'],
  },
  questionCount: 12,
}

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  vi.mocked(client.getExams).mockResolvedValue([{ code: 'SAA-C03', name: exam.name, version: exam.version }])
  vi.mocked(client.getExam).mockResolvedValue(exam)
  vi.mocked(client.getTopic).mockResolvedValue(topic)
})

describe('Dashboard', () => {
  it('lists exams as links', async () => {
    renderAt('/')
    const link = await screen.findByRole('link', { name: /SAA-C03/ })
    expect(link).toHaveAttribute('href', '/exams/SAA-C03')
  })

  it('shows an empty state when nothing is imported', async () => {
    vi.mocked(client.getExams).mockResolvedValue([])
    renderAt('/')
    expect(await screen.findByText(/no exams/i)).toBeInTheDocument()
  })

  it('shows an error when the API is unreachable', async () => {
    vi.mocked(client.getExams).mockRejectedValue(new client.ApiError(0, 'Cannot reach the API.'))
    renderAt('/')
    expect(await screen.findByRole('alert')).toHaveTextContent('Cannot reach the API.')
  })
})

describe('Exam page', () => {
  it('shows domains with weights and topics with question counts', async () => {
    renderAt('/exams/SAA-C03')
    expect(await screen.findByRole('heading', { name: /Design Secure Architectures/ })).toBeInTheDocument()
    expect(screen.getByText(/30%/)).toBeInTheDocument()
    const link = screen.getByRole('link', { name: /S3 security and access/ })
    expect(link).toHaveAttribute('href', '/topics/7')
    expect(link).toHaveTextContent('12 questions')
  })

  it('shows a not-found message on 404', async () => {
    vi.mocked(client.getExam).mockRejectedValue(new client.ApiError(404, 'Exam not found.'))
    renderAt('/exams/NOPE')
    expect(await screen.findByRole('alert')).toHaveTextContent('Exam not found.')
  })
})

describe('Topic page', () => {
  it('renders the lesson as sanitized markdown', async () => {
    const { container } = renderAt('/topics/7')
    expect(await screen.findByRole('heading', { name: 'S3 security and access' })).toBeInTheDocument()
    expect(screen.getByText('bucket policies').tagName).toBe('STRONG')
    expect(container.querySelector('script')).toBeNull()
    expect(screen.getByText(/Bucket policies vs IAM policies/)).toBeInTheDocument()
  })

  it('only links https documentation URLs', async () => {
    renderAt('/topics/7')
    await screen.findByRole('heading', { name: 'S3 security and access' })
    const links = screen.getAllByRole('link', { name: /docs\.aws\.amazon\.com/ })
    expect(links).toHaveLength(1)
    expect(links[0]).toHaveAttribute('rel', 'noopener noreferrer')
    expect(screen.queryByRole('link', { name: /javascript/ })).toBeNull()
  })

  it('handles a topic without a lesson', async () => {
    vi.mocked(client.getTopic).mockResolvedValue({ ...topic, lesson: null })
    renderAt('/topics/7')
    expect(await screen.findByText(/no lesson/i)).toBeInTheDocument()
  })

  it('offers navigation back to the dashboard', async () => {
    renderAt('/topics/7')
    await screen.findByRole('heading', { name: 'S3 security and access' })
    expect(screen.getByRole('link', { name: /dashboard/i })).toHaveAttribute('href', '/')
  })
})

describe('Navigation', () => {
  it('shows a not-found page for unknown routes', () => {
    renderAt('/nope')
    expect(screen.getByRole('heading', { name: /not found/i })).toBeInTheDocument()
  })
})

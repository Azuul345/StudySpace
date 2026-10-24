import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from '../App'
import * as client from '../api/client'
import { Diagram } from './Diagram'
import { diagramRegistry } from './registry'

vi.mock('../api/client', async (orig) => ({ ...(await orig<typeof client>()), getTopic: vi.fn() }))

const topic = (diagrams?: string[]): client.TopicDetail => ({
  id: 7, slug: 'x', name: 'Network', examCode: 'SAA-C03', domain: { id: 1, name: 'Secure' },
  lesson: { summary: 'Summary.', keyPoints: ['Point'], docLinks: ['https://docs.aws.amazon.com/x'], diagrams },
  questionCount: 0,
})

const renderTopic = () => render(<MemoryRouter initialEntries={['/topics/7']}><App /></MemoryRouter>)

beforeEach(() => vi.clearAllMocks())

describe('diagram registry (REQ-VIS-001)', () => {
  it.each(Object.entries(diagramRegistry))('%s has a title, a description and a viewBox', (_id, def) => {
    expect(def.title.length).toBeGreaterThan(5)
    expect(def.description.length).toBeGreaterThan(40)
    expect(def.viewBox).toMatch(/^0 0 \d+ \d+$/)
  })

  it.each(Object.entries(diagramRegistry))('%s renders a named image with no script or foreign content', (_id, def) => {
    const { container } = render(<Diagram def={def} />)

    expect(screen.getByRole('img', { name: def.title })).toBeInTheDocument()
    expect(container.querySelector('script, foreignObject, image, a')).toBeNull()
    expect(container.querySelectorAll('text').length).toBeGreaterThan(2)
  })
})

describe('lesson page diagrams', () => {
  it('draws the diagrams the lesson lists, in order', async () => {
    vi.mocked(client.getTopic).mockResolvedValue(topic(['sqs-decoupling', 'vpc-public-private']))
    renderTopic()

    const images = await screen.findAllByRole('img')
    expect(images.map((i) => i.querySelector('title')?.textContent)).toEqual([
      diagramRegistry['sqs-decoupling'].title,
      diagramRegistry['vpc-public-private'].title,
    ])
  })

  it('skips ids the app does not know and still shows the lesson', async () => {
    vi.mocked(client.getTopic).mockResolvedValue(topic(['not-a-real-diagram']))
    renderTopic()

    expect(await screen.findByText('Summary.')).toBeInTheDocument()
    expect(screen.queryByRole('img')).toBeNull()
  })

  it('works for a lesson with no diagrams field', async () => {
    vi.mocked(client.getTopic).mockResolvedValue(topic(undefined))
    renderTopic()

    expect(await screen.findByText('Summary.')).toBeInTheDocument()
    expect(screen.queryByRole('img')).toBeNull()
  })
})
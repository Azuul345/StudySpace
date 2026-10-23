import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Question } from '../api/client'
import { Flashcard } from './Flashcard'
import { Matching } from './Matching'
import { TypedAnswer } from './TypedAnswer'

const base: Question = {
  id: 1, type: 'mcq', prompt: 'Pick one', difficulty: 1, tags: [], topicId: 1,
  scenario: null, options: null, pairs: null, steps: null,
}

describe('Matching', () => {
  const q = { ...base, type: 'matching' as const, pairs: { lefts: ['EC2', 'S3'], rights: ['Object storage', 'Virtual servers'] } }
  it('requires every left item to be matched, then submits pairs', async () => {
    const onSubmit = vi.fn()
    render(<Matching question={q} onSubmit={onSubmit} />)
    expect(screen.getByRole('button', { name: /submit/i })).toBeDisabled()
    await userEvent.selectOptions(screen.getByLabelText('EC2'), 'Virtual servers')
    await userEvent.selectOptions(screen.getByLabelText('S3'), 'Object storage')
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({
      pairs: [{ left: 'EC2', right: 'Virtual servers' }, { left: 'S3', right: 'Object storage' }],
    })
  })
})

describe('TypedAnswer', () => {
  const q = { ...base, type: 'typed' as const, prompt: 'Name the service.' }
  it('submits trimmed text and ignores blank input', async () => {
    const onSubmit = vi.fn()
    render(<TypedAnswer question={q} onSubmit={onSubmit} />)
    const box = screen.getByRole('textbox')
    await userEvent.type(box, '   ')
    expect(screen.getByRole('button', { name: /submit/i })).toBeDisabled()
    await userEvent.type(box, ' Amazon S3 ')
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ text: 'Amazon S3' })
  })
  it('caps input at the 2000 character server limit', () => {
    render(<TypedAnswer question={q} onSubmit={vi.fn()} />)
    expect(screen.getByRole('textbox')).toHaveAttribute('maxlength', '2000')
  })
})

describe('Flashcard', () => {
  const card = { id: 5, term: 'Bucket', definition: 'A container for objects', isNew: true }
  it('hides the definition until revealed, then reports the quality', async () => {
    const onRate = vi.fn()
    render(<Flashcard card={card} onRate={onRate} />)
    expect(screen.queryByText('A container for objects')).toBeNull()
    await userEvent.click(screen.getByRole('button', { name: /show answer/i }))
    expect(screen.getByText('A container for objects')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Good' }))
    expect(onRate).toHaveBeenCalledWith(4)
  })
  it('maps Again/Hard/Easy to SM-2 qualities', async () => {
    const onRate = vi.fn()
    render(<Flashcard card={card} onRate={onRate} />)
    await userEvent.click(screen.getByRole('button', { name: /show answer/i }))
    for (const [label, quality] of [['Again', 1], ['Hard', 3], ['Easy', 5]] as const) {
      await userEvent.click(screen.getByRole('button', { name: label }))
      expect(onRate).toHaveBeenLastCalledWith(quality)
    }
  })
})

import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { Question } from '../api/client'
import { MultipleChoice } from './MultipleChoice'
import { MultiResponse } from './MultiResponse'
import { Ordering } from './Ordering'
import { Scenario } from './Scenario'
import { TrueFalse } from './TrueFalse'

const base: Question = {
  id: 1, type: 'mcq', prompt: 'Pick one', difficulty: 1, tags: [], topicId: 1,
  scenario: null, options: null, pairs: null, steps: null,
}
const options = [
  { id: 'a', text: 'Alpha' },
  { id: 'b', text: 'Beta' },
  { id: 'c', text: 'Gamma' },
]

describe('MultipleChoice', () => {
  const q = { ...base, options }
  it('submits the single chosen option', async () => {
    const onSubmit = vi.fn()
    render(<MultipleChoice question={q} onSubmit={onSubmit} />)
    expect(screen.getByRole('button', { name: /submit/i })).toBeDisabled()
    await userEvent.click(screen.getByRole('radio', { name: 'Beta' }))
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ selected: ['b'] })
  })
  it('locks inputs when disabled', () => {
    render(<MultipleChoice question={q} onSubmit={vi.fn()} disabled />)
    expect(screen.getByRole('radio', { name: 'Alpha' })).toBeDisabled()
  })
})

describe('MultiResponse', () => {
  const q = { ...base, type: 'multi' as const, options }
  it('submits every ticked option', async () => {
    const onSubmit = vi.fn()
    render(<MultiResponse question={q} onSubmit={onSubmit} />)
    await userEvent.click(screen.getByRole('checkbox', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('checkbox', { name: 'Gamma' }))
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ selected: ['a', 'c'] })
  })
  it('can untick an option', async () => {
    render(<MultiResponse question={q} onSubmit={vi.fn()} />)
    await userEvent.click(screen.getByRole('checkbox', { name: 'Alpha' }))
    await userEvent.click(screen.getByRole('checkbox', { name: 'Alpha' }))
    expect(screen.getByRole('button', { name: /submit/i })).toBeDisabled()
  })
})

describe('Scenario', () => {
  it('shows the scenario text above the options', async () => {
    const onSubmit = vi.fn()
    const q = { ...base, type: 'scenario' as const, options, scenario: { id: 9, text: 'A company runs a web app.' } }
    render(<Scenario question={q} onSubmit={onSubmit} />)
    expect(screen.getByText('A company runs a web app.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('checkbox', { name: 'Beta' }))
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ selected: ['b'] })
  })
})

describe('TrueFalse', () => {
  it('submits the boolean', async () => {
    const onSubmit = vi.fn()
    render(<TrueFalse question={{ ...base, type: 'truefalse', prompt: 'S3 is regional.' }} onSubmit={onSubmit} />)
    await userEvent.click(screen.getByRole('radio', { name: 'False' }))
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ value: false })
  })
})

describe('Ordering', () => {
  const q = { ...base, type: 'ordering' as const, steps: ['First', 'Second', 'Third'] }
  it('submits the initial order untouched as indexes', async () => {
    const onSubmit = vi.fn()
    render(<Ordering question={q} onSubmit={onSubmit} />)
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ order: [0, 1, 2] })
  })
  it('moves steps up and down', async () => {
    const onSubmit = vi.fn()
    render(<Ordering question={q} onSubmit={onSubmit} />)
    await userEvent.click(screen.getByRole('button', { name: 'Move Third up' }))
    await userEvent.click(screen.getByRole('button', { name: 'Move First down' }))
    const text = screen.getAllByRole('listitem').map((li) => li.textContent ?? '')
    expect(text[0]).toContain('Third')
    expect(text[1]).toContain('First')
    expect(text[2]).toContain('Second')
    await userEvent.click(screen.getByRole('button', { name: /submit/i }))
    expect(onSubmit).toHaveBeenCalledWith({ order: [2, 0, 1] })
  })
  it('cannot move the first step up', () => {
    render(<Ordering question={q} onSubmit={vi.fn()} />)
    expect(screen.getByRole('button', { name: 'Move First up' })).toBeDisabled()
  })
})

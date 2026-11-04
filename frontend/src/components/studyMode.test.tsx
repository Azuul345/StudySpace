import { fireEvent, render, renderHook, screen, act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Mnemonic } from './Mnemonic'
import { ModeSwitch } from './ModeSwitch'
import { readMode, useStudyMode, writeMode } from './studyMode'

describe('study mode choice', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('starts in professional wording when nothing is stored', () => {
    expect(readMode()).toBe('professional')
  })

  it('remembers the simple choice for the next visit', () => {
    writeMode('simple')
    expect(readMode()).toBe('simple')
  })

  it('still works when browser storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })

    expect(readMode()).toBe('professional')
    const { result } = renderHook(() => useStudyMode())
    act(() => result.current[1]('simple'))
    expect(result.current[0]).toBe('simple')
  })
})

describe('ModeSwitch', () => {
  it('marks the current style and reports a change', () => {
    const onChange = vi.fn()
    render(<ModeSwitch mode="professional" onChange={onChange} />)

    expect(screen.getByRole('button', { name: 'Professional' })).toHaveAttribute('aria-pressed', 'true')
    fireEvent.click(screen.getByRole('button', { name: 'Simple' }))
    expect(onChange).toHaveBeenCalledWith('simple')
  })
})

describe('Mnemonic', () => {
  it('shows nothing when there is no mnemonic', () => {
    const { container } = render(<Mnemonic text={null} />)
    expect(container).toBeEmptyDOMElement()
  })

  it('shows the memory hook when there is one', () => {
    render(<Mnemonic text="Naruto hint: a test." />)
    expect(screen.getByText('Naruto hint: a test.')).toBeInTheDocument()
  })
})

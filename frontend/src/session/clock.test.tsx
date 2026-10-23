import { act, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Countdown } from './Countdown'
import { formatClock } from './formatClock'

describe('formatClock', () => {
  it('formats h:mm:ss only when needed', () => {
    expect(formatClock(130 * 60)).toBe('2:10:00')
    expect(formatClock(59 * 60 + 5)).toBe('59:05')
    expect(formatClock(0)).toBe('0:00')
    expect(formatClock(-5)).toBe('0:00')
  })
})

describe('Countdown', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-10-23T12:00:00Z'))
  })
  afterEach(() => vi.useRealTimers())

  it('counts down from the deadline', () => {
    render(<Countdown deadline={Date.now() + 130 * 60_000} onExpire={vi.fn()} />)
    expect(screen.getByRole('timer')).toHaveTextContent('2:10:00')
    act(() => vi.advanceTimersByTime(61_000))
    expect(screen.getByRole('timer')).toHaveTextContent('2:08:59')
  })

  it('expires exactly once at 130 minutes', () => {
    const onExpire = vi.fn()
    render(<Countdown deadline={Date.now() + 130 * 60_000} onExpire={onExpire} />)
    act(() => vi.advanceTimersByTime(130 * 60_000 - 1_000))
    expect(onExpire).not.toHaveBeenCalled()
    act(() => vi.advanceTimersByTime(1_000))
    expect(onExpire).toHaveBeenCalledTimes(1)
    act(() => vi.advanceTimersByTime(60_000))
    expect(onExpire).toHaveBeenCalledTimes(1)
    expect(screen.getByRole('timer')).toHaveTextContent('0:00')
  })
})

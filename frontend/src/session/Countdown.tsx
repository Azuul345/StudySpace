import { useEffect, useRef, useState } from 'react'
import { formatClock } from './formatClock'

/// Counts down to `deadline` (epoch ms) and calls `onExpire` once when it is reached.
export function Countdown({ deadline, onExpire }: { deadline: number; onExpire: () => void }) {
  const [remaining, setRemaining] = useState(() => Math.ceil((deadline - Date.now()) / 1000))
  const expired = useRef(false)
  const onExpireRef = useRef(onExpire)

  useEffect(() => {
    onExpireRef.current = onExpire
  }, [onExpire])

  useEffect(() => {
    const id = setInterval(() => {
      const left = Math.ceil((deadline - Date.now()) / 1000)
      setRemaining(left)
      if (left <= 0 && !expired.current) {
        expired.current = true
        onExpireRef.current()
      }
    }, 1000)
    return () => clearInterval(id)
  }, [deadline])

  return (
    <div role="timer" aria-label="Time remaining" className={remaining <= 300 ? 'timer low' : 'timer'}>
      {formatClock(remaining)}
    </div>
  )
}

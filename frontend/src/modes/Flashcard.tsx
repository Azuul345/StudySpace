import { useState } from 'react'
import type { Flashcard as Card } from '../api/client'

// Anki-style buttons mapped onto SM-2 quality 0-5 (below 3 resets the card).
const RATINGS = [
  ['Again', 1],
  ['Hard', 3],
  ['Good', 4],
  ['Easy', 5],
] as const

export function Flashcard({ card, onRate }: { card: Card; onRate: (quality: number) => void }) {
  const [revealed, setRevealed] = useState(false)
  return (
    <article className="flashcard">
      <h2>{card.term}</h2>
      {revealed ? (
        <>
          <p>{card.definition}</p>
          <div className="ratings">
            {RATINGS.map(([label, quality]) => (
              <button key={label} type="button" onClick={() => onRate(quality)}>
                {label}
              </button>
            ))}
          </div>
        </>
      ) : (
        <button type="button" onClick={() => setRevealed(true)}>
          Show answer
        </button>
      )}
    </article>
  )
}

import { useCallback, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, getDueFlashcards, reviewFlashcard } from '../api/client'
import { Async } from '../components/Async'
import { useLoad } from '../components/useLoad'
import { Flashcard } from '../modes/Flashcard'
import type { Flashcard as Card } from '../api/client'

function Deck({ cards, examCode }: { cards: Card[]; examCode: string }) {
  const [index, setIndex] = useState(0)
  const [error, setError] = useState<string | null>(null)

  if (cards.length === 0) return <p>No flashcards are due. Come back later.</p>
  if (index >= cards.length) {
    return (
      <p>
        All done for now. <Link to={`/exams/${encodeURIComponent(examCode)}`}>Back to the exam</Link>
      </p>
    )
  }

  const card = cards[index]
  const rate = async (quality: number) => {
    setError(null)
    try {
      await reviewFlashcard(card.id, quality)
      setIndex(index + 1)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Something went wrong.')
    }
  }

  return (
    <>
      <p className="progress">
        Card {index + 1} of {cards.length}
      </p>
      <Flashcard key={card.id} card={card} onRate={(q) => void rate(q)} />
      {error && <p role="alert" className="error">{error}</p>}
    </>
  )
}

export function FlashcardsPage() {
  const { code = '' } = useParams()
  const due = useLoad(useCallback(() => getDueFlashcards(code), [code]))
  return (
    <section>
      <h1>Flashcards</h1>
      <Async state={due}>{(cards) => <Deck cards={cards} examCode={code} />}</Async>
    </section>
  )
}

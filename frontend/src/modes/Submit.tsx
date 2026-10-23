export function Submit({ ready, disabled }: { ready: boolean; disabled?: boolean }) {
  return (
    <button type="submit" disabled={!ready || disabled}>
      Submit answer
    </button>
  )
}

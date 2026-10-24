/// Shown when a verifier could not fully confirm a question. Absent caveat renders nothing.
export function Caveat({ text }: { text?: string | null }) {
  if (!text) {
    return null
  }
  return (
    <p className="caveat" role="note">
      <strong>Not fully verified.</strong> {text}
    </p>
  )
}

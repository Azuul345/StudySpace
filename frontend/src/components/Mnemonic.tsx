/// A memory hook in plain text. Nothing is shown when the pack has no mnemonic for the item.
export function Mnemonic({ text }: { text?: string | null }) {
  if (!text) return null
  return (
    <aside className="mnemonic">
      <p>{text}</p>
    </aside>
  )
}

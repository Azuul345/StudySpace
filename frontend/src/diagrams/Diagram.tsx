import { useId } from 'react'
import type { DiagramDef } from './types'

/// Frame for one diagram: an SVG with a title and description for assistive tech, and the description as a visible caption.
export function Diagram({ def }: { def: DiagramDef }) {
  const id = useId()
  const { Body } = def
  return (
    <figure className="diagram">
      <svg viewBox={def.viewBox} role="img" aria-labelledby={`${id}-t`} aria-describedby={`${id}-d`} xmlns="http://www.w3.org/2000/svg">
        <title id={`${id}-t`}>{def.title}</title>
        <desc id={`${id}-d`}>{def.description}</desc>
        <defs>
          <marker id="ss-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
            <path d="M0 0 L10 5 L0 10 z" fill="currentColor" />
          </marker>
        </defs>
        <Body />
      </svg>
      <figcaption>
        <strong>{def.title}.</strong> {def.description}
      </figcaption>
    </figure>
  )
}

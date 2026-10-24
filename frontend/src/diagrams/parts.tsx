import type { ReactNode } from 'react'

/// Drawing blocks shared by every diagram. Everything uses currentColor so it follows light and dark themes.

export function Group({ x, y, w, h, label }: { x: number; y: number; w: number; h: number; label: string }) {
  return (
    <g>
      <rect className="dg-group" x={x} y={y} width={w} height={h} rx={8} />
      <text className="dg-group-label" x={x + 10} y={y + 18}>
        {label}
      </text>
    </g>
  )
}

export function Box({ x, y, w, h, label, sub, accent }: { x: number; y: number; w: number; h: number; label: string; sub?: string; accent?: boolean }) {
  const cx = x + w / 2
  return (
    <g>
      <rect className={accent ? 'dg-box dg-accent' : 'dg-box'} x={x} y={y} width={w} height={h} rx={6} />
      <text className="dg-label" x={cx} y={sub ? y + h / 2 - 3 : y + h / 2 + 5} textAnchor="middle">
        {label}
      </text>
      {sub && (
        <text className="dg-sub" x={cx} y={y + h / 2 + 15} textAnchor="middle">
          {sub}
        </text>
      )}
    </g>
  )
}

export function Arrow({ from, to, dashed, noHead }: { from: [number, number]; to: [number, number]; dashed?: boolean; noHead?: boolean }) {
  return (
    <line
      className={dashed ? 'dg-arrow dg-dashed' : 'dg-arrow'}
      x1={from[0]}
      y1={from[1]}
      x2={to[0]}
      y2={to[1]}
      markerEnd={noHead ? undefined : 'url(#ss-arrow)'}
    />
  )
}

export function Note({ x, y, anchor = 'start', children }: { x: number; y: number; anchor?: 'start' | 'middle' | 'end'; children: ReactNode }) {
  return (
    <text className="dg-sub" x={x} y={y} textAnchor={anchor}>
      {children}
    </text>
  )
}

/// A numbered marker that ties an arrow to a line in Steps.
export function Badge({ x, y, n }: { x: number; y: number; n: number }) {
  return (
    <g>
      <circle className="dg-badge" cx={x} cy={y} r={9} />
      <text className="dg-badge-text" x={x} y={y + 4} textAnchor="middle">
        {n}
      </text>
    </g>
  )
}

/// Numbered explanation lines under a diagram.
export function Steps({ x, y, items }: { x: number; y: number; items: string[] }) {
  return (
    <g>
      {items.map((text, i) => (
        <g key={text}>
          <Badge x={x + 9} y={y + i * 22 - 4} n={i + 1} />
          <text className="dg-sub dg-step" x={x + 26} y={y + i * 22}>
            {text}
          </text>
        </g>
      ))}
    </g>
  )
}

import { Box, Note } from './parts'

const columns = [
  { x: 10, label: 'Provisioned', sub: 'you set the capacity', rows: ['You set read and write capacity units', 'Can be cheaper for steady, well-used capacity', 'Can use auto scaling to adjust capacity'], accent: false },
  { x: 330, label: 'On-demand', sub: 'pay per request', rows: ['Pay for each read and write request', 'No capacity to plan or provision', 'Suits spiky or unknown traffic'], accent: true },
]

export function DynamoCapacityModesBody() {
  return (
    <>
      {columns.map((c) => (
        <g key={c.label}>
          <Box x={c.x} y={15} w={300} h={60} label={c.label} sub={c.sub} accent={c.accent} />
          {c.rows.map((r, i) => (
            <Note key={r} x={c.x + 8} y={105 + i * 22}>
              {r}
            </Note>
          ))}
        </g>
      ))}
    </>
  )
}

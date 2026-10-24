import { Box, Note } from './parts'

const bands = [
  { y: 15, label: 'Spot Instances', sub: 'spare capacity, can be interrupted', notes: ['Batch jobs and stateless workers', 'that tolerate interruption'], accent: false },
  { y: 90, label: 'On-Demand', sub: 'no commitment, pay for what you use', notes: ['Spiky or unpredictable demand', 'and short-term needs'], accent: false },
  { y: 165, label: 'Savings Plans or Reserved', sub: 'commit for 1 or 3 years', notes: ['Steady, always-on baseline', 'such as production servers'], accent: true },
]

export function Ec2PurchaseOptionsBody() {
  return (
    <>
      {bands.map((b) => (
        <g key={b.label}>
          <Box x={10} y={b.y} w={320} h={60} label={b.label} sub={b.sub} accent={b.accent} />
          <Note x={350} y={b.y + 27}>{b.notes[0]}</Note>
          <Note x={350} y={b.y + 45}>{b.notes[1]}</Note>
        </g>
      ))}
      <Note x={10} y={252}>Common pattern: commit for the steady baseline, then add On-Demand and Spot above it.</Note>
    </>
  )
}

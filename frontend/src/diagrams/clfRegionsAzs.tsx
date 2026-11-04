import { Arrow, Box, Group, Note } from './parts'

export function ClfRegionsAzsBody() {
  return (
    <>
      <Box x={10} y={100} w={120} h={60} label="Customers" sub="users anywhere" />
      <Group x={180} y={20} w={440} h={250} label="AWS Region" />
      <Box x={200} y={70} w={120} h={150} label="Availability Zone A" sub="one or more data centers" accent />
      <Box x={340} y={70} w={120} h={150} label="Availability Zone B" sub="separate power and network" />
      <Box x={480} y={70} w={120} h={150} label="Availability Zone C" sub="separate facilities" />
      <Arrow from={[130, 130]} to={[200, 130]} />
      <Note x={190} y={248} anchor="start">Every Region has at least three zones.</Note>
      <Note x={190} y={264}>An app spread across zones keeps running if one zone fails.</Note>
    </>
  )
}

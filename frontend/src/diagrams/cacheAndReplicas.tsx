import { Arrow, Badge, Box, Note } from './parts'

export function CacheAndReplicasBody() {
  return (
    <>
      <Box x={10} y={95} w={110} h={60} label="Application" />
      <Box x={230} y={15} w={150} h={55} label="ElastiCache" sub="hot reads from memory" accent />
      <Box x={230} y={100} w={150} h={55} label="Read replicas" sub="scale read traffic" />
      <Box x={230} y={185} w={150} h={55} label="Primary database" sub="all writes" />
      <Arrow from={[120, 108]} to={[230, 52]} />
      <Badge x={165} y={66} n={1} />
      <Arrow from={[120, 125]} to={[230, 127]} />
      <Badge x={175} y={114} n={2} />
      <Arrow from={[120, 142]} to={[230, 205]} />
      <Badge x={165} y={189} n={3} />
      <Arrow from={[305, 185]} to={[305, 155]} dashed />
      <Note x={395} y={45}>1. Check the cache first</Note>
      <Note x={395} y={125}>2. On a miss, read from a replica</Note>
      <Note x={395} y={212}>3. Send writes to the primary</Note>
      <Note x={395} y={172}>Asynchronous replication (can lag)</Note>
    </>
  )
}

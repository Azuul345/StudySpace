import { Arrow, Badge, Box, Steps } from './parts'

export function CloudfrontEdgeBody() {
  return (
    <>
      <Box x={10} y={60} w={100} h={60} label="Users" />
      <Box x={250} y={60} w={140} h={60} label="CloudFront" sub="edge location cache" accent />
      <Box x={530} y={60} w={100} h={60} label="Origin" sub="S3 or ALB" />
      <Arrow from={[110, 80]} to={[250, 80]} />
      <Badge x={180} y={64} n={1} />
      <Arrow from={[250, 105]} to={[110, 105]} dashed />
      <Badge x={180} y={124} n={2} />
      <Arrow from={[390, 80]} to={[530, 80]} />
      <Badge x={460} y={64} n={3} />
      <Arrow from={[530, 105]} to={[390, 105]} dashed />
      <Badge x={460} y={124} n={4} />
      <Steps
        x={10}
        y={170}
        items={[
          'A user requests an object and is routed to a nearby edge location',
          'Cache hit: the edge returns its cached copy right away',
          'Cache miss: the edge asks the origin (often via a regional edge cache)',
          'The origin responds; the edge caches the object and returns it',
        ]}
      />
    </>
  )
}

import { Arrow, Box, Group, Note } from './parts'

export function VpcPublicPrivateBody() {
  return (
    <>
      <Box x={10} y={135} w={80} h={50} label="Internet" />
      <Box x={115} y={135} w={95} h={50} label="Internet" sub="gateway" />
      <Group x={230} y={10} w={400} h={310} label="VPC" />
      <Group x={250} y={40} w={150} h={265} label="Public subnet" />
      <Group x={450} y={40} w={165} h={265} label="Private subnet" />
      <Box x={265} y={70} w={120} h={50} label="Load balancer" />
      <Box x={265} y={215} w={120} h={50} label="NAT gateway" />
      <Box x={470} y={135} w={125} h={50} label="EC2 instances" accent />
      <Arrow from={[90, 150]} to={[115, 150]} />
      <Arrow from={[115, 172]} to={[90, 172]} dashed />
      <Arrow from={[210, 150]} to={[265, 105]} />
      <Arrow from={[385, 95]} to={[470, 145]} />
      <Arrow from={[470, 178]} to={[385, 228]} dashed />
      <Arrow from={[265, 240]} to={[210, 180]} dashed />
      <Arrow from={[10, 345]} to={[40, 345]} />
      <Note x={48} y={349}>Inbound request path</Note>
      <Arrow from={[250, 345]} to={[280, 345]} dashed />
      <Note x={288} y={349}>Outbound-only path through the NAT gateway</Note>
    </>
  )
}

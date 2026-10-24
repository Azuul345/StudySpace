import { Arrow, Box, Note } from './parts'

export function EndpointVsNatBody() {
  return (
    <>
      <Box x={10} y={95} w={120} h={60} label="EC2 instance" sub="private subnet" />
      <Box x={190} y={20} w={110} h={50} label="NAT gateway" />
      <Box x={350} y={20} w={100} h={50} label="Internet" sub="gateway" />
      <Box x={190} y={185} w={130} h={50} label="Gateway endpoint" sub="for S3 and DynamoDB" accent />
      <Box x={520} y={95} w={110} h={60} label="Amazon S3" />
      <Arrow from={[130, 105]} to={[190, 55]} />
      <Arrow from={[300, 45]} to={[350, 45]} />
      <Arrow from={[450, 50]} to={[520, 105]} />
      <Arrow from={[130, 145]} to={[190, 205]} />
      <Arrow from={[320, 210]} to={[520, 148]} />
      <Note x={190} y={90}>NAT gateway: hourly charge plus</Note>
      <Note x={190} y={108}>per-GB data processing</Note>
      <Note x={345} y={238}>No additional charge for the endpoint</Note>
    </>
  )
}
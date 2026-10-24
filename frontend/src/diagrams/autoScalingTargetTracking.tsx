import { Arrow, Box, Group, Note } from './parts'

export function AutoScalingTargetTrackingBody() {
  return (
    <>
      <Box x={10} y={65} w={120} h={60} label="Load balancer" sub="spreads traffic" />
      <Group x={165} y={40} w={150} h={110} label="Auto Scaling group" />
      <Box x={180} y={72} w={120} h={60} label="EC2 instances" sub="added or removed" accent />
      <Box x={355} y={65} w={120} h={60} label="CloudWatch" sub="instance metrics" />
      <Box x={515} y={65} w={115} h={60} label="Scaling policy" sub="target tracking" />
      <Arrow from={[130, 95]} to={[180, 102]} />
      <Arrow from={[300, 102]} to={[355, 95]} dashed />
      <Arrow from={[475, 95]} to={[515, 95]} />
      <Arrow from={[572, 125]} to={[572, 185]} noHead />
      <Arrow from={[572, 185]} to={[240, 185]} noHead />
      <Arrow from={[240, 185]} to={[240, 132]} />
      <Note x={410} y={205} anchor="middle">keeps the metric near the target by adding or removing instances</Note>
    </>
  )
}

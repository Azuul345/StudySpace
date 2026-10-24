import { Arrow, Box, Group } from './parts'

export function MultiAzWebBody() {
  return (
    <>
      <Box x={10} y={10} w={90} h={40} label="Users" />
      <Box x={250} y={10} w={380} h={40} label="Load balancer" sub="spans both Availability Zones" />
      <Arrow from={[100, 30]} to={[250, 30]} />
      <Group x={250} y={80} w={185} h={215} label="Availability Zone A" />
      <Group x={445} y={80} w={185} h={215} label="Availability Zone B" />
      <Box x={265} y={115} w={155} h={50} label="Web servers" sub="Auto Scaling group" />
      <Box x={460} y={115} w={155} h={50} label="Web servers" sub="Auto Scaling group" />
      <Box x={265} y={215} w={155} h={55} label="RDS primary" sub="reads and writes" accent />
      <Box x={460} y={215} w={155} h={55} label="RDS standby" sub="synchronous copy" />
      <Arrow from={[400, 50]} to={[400, 115]} />
      <Arrow from={[595, 50]} to={[595, 115]} />
      <Arrow from={[400, 165]} to={[400, 215]} />
      <Arrow from={[480, 165]} to={[420, 228]} />
      <Arrow from={[420, 243]} to={[460, 243]} dashed />
    </>
  )
}

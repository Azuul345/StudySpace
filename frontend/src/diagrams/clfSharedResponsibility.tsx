import { Box, Group, Note } from './parts'

export function ClfSharedResponsibilityBody() {
  return (
    <>
      <Group x={10} y={15} w={305} h={200} label="Security IN the cloud (customer)" />
      <Box x={30} y={50} w={265} h={36} label="Your data and who can access it" />
      <Box x={30} y={95} w={265} h={36} label="Operating system and updates (on EC2)" />
      <Box x={30} y={140} w={265} h={36} label="Firewall rules and your application" />
      <Group x={325} y={15} w={305} h={200} label="Security OF the cloud (AWS)" />
      <Box x={345} y={50} w={265} h={36} label="Data center buildings and power" accent />
      <Box x={345} y={95} w={265} h={36} label="Hardware and the global network" accent />
      <Box x={345} y={140} w={265} h={36} label="Software that runs managed services" accent />
      <Note x={10} y={250}>The split moves with the service: with managed services AWS takes on more of the work.</Note>
    </>
  )
}

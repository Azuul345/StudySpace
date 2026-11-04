import { Box, Group, Note } from './parts'

export function ClfContainersVsVmsBody() {
  return (
    <>
      <Group x={10} y={15} w={300} h={220} label="Virtual machines" />
      <Box x={30} y={50} w={260} h={34} label="App A" />
      <Box x={30} y={92} w={260} h={34} label="Guest operating system" sub="a full copy per machine" />
      <Box x={30} y={134} w={260} h={34} label="Hypervisor" />
      <Box x={30} y={176} w={260} h={34} label="Physical server" />
      <Group x={330} y={15} w={300} h={220} label="Containers" accent />
      <Box x={350} y={50} w={260} h={34} label="App A" accent />
      <Box x={350} y={92} w={260} h={34} label="Container engine" sub="shares the host OS" accent />
      <Box x={350} y={134} w={260} h={34} label="Host operating system" accent />
      <Box x={350} y={176} w={260} h={34} label="Physical server" accent />
      <Note x={10} y={262}>Containers package only what one app needs, so their images are much smaller than VM images.</Note>
    </>
  )
}

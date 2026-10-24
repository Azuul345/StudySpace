import { Arrow, Box, Note } from './parts'

export function SqsDecouplingBody() {
  return (
    <>
      <Box x={10} y={60} w={140} h={60} label="Web tier" sub="producers" />
      <Box x={250} y={60} w={140} h={60} label="SQS queue" sub="buffers messages" accent />
      <Box x={490} y={60} w={140} h={60} label="Worker fleet" sub="Auto Scaling group" />
      <Arrow from={[150, 90]} to={[250, 90]} />
      <Note x={200} y={80} anchor="middle">send</Note>
      <Arrow from={[390, 90]} to={[490, 90]} />
      <Note x={440} y={80} anchor="middle">poll, process</Note>
      <Box x={250} y={150} w={140} h={44} label="Queue backlog" sub="CloudWatch metric" />
      <Arrow from={[320, 120]} to={[320, 150]} dashed />
      <Arrow from={[390, 172]} to={[540, 122]} dashed />
      <Note x={400} y={205}>scale out on backlog per instance</Note>
    </>
  )
}

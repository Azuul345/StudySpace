import { Arrow, Box, Note } from './parts'

export function StreamingIngestionBody() {
  return (
    <>
      <Box x={10} y={30} w={110} h={60} label="Producers" sub="apps, IoT devices" />
      <Box x={160} y={30} w={130} h={60} label="Kinesis" sub="Data Streams" accent />
      <Box x={330} y={30} w={130} h={60} label="Amazon Data" sub="Firehose" />
      <Box x={500} y={30} w={130} h={60} label="Amazon S3" sub="data lake" />
      <Box x={160} y={140} w={130} h={55} label="Custom consumers" sub="Lambda, EC2" />
      <Box x={500} y={140} w={130} h={55} label="Athena" sub="SQL on S3 data" />
      <Arrow from={[120, 60]} to={[160, 60]} />
      <Arrow from={[290, 60]} to={[330, 60]} />
      <Arrow from={[460, 60]} to={[500, 60]} />
      <Arrow from={[225, 90]} to={[225, 140]} dashed />
      <Arrow from={[565, 90]} to={[565, 140]} />
      <Note x={330} y={165}>Firehose loads the stream into S3;</Note>
      <Note x={330} y={183}>Athena queries it in place</Note>
    </>
  )
}

import { Arrow, Box, Note } from './parts'

export function S3StorageLifecycleBody() {
  return (
    <>
      <Note x={320} y={28} anchor="middle">Lifecycle rules move objects to colder classes as they age</Note>
      <Box x={10} y={50} w={120} h={60} label="S3 Standard" sub="frequent access" accent />
      <Box x={170} y={50} w={120} h={60} label="S3 Standard-IA" sub="infrequent access" />
      <Box x={330} y={50} w={120} h={60} label="Glacier Flexible" sub="Retrieval (archive)" />
      <Box x={490} y={50} w={120} h={60} label="Glacier Deep" sub="Archive (coldest)" />
      <Arrow from={[130, 80]} to={[170, 80]} />
      <Arrow from={[290, 80]} to={[330, 80]} />
      <Arrow from={[450, 80]} to={[490, 80]} />
      <Note x={10} y={150}>Storage price per GB falls</Note>
      <Arrow from={[10, 158]} to={[610, 158]} dashed />
      <Note x={10} y={188}>Retrieval takes longer and costs more</Note>
      <Arrow from={[10, 196]} to={[610, 196]} dashed />
    </>
  )
}

import { Box, Note } from './parts'

const columns = [
  { x: 10, label: 'Block: EBS', sub: 'a disk for an instance', rows: ['Attached to one instance', 'Lives in a single AZ', 'Databases, boot volumes'], accent: false },
  { x: 225, label: 'File: EFS', sub: 'a shared file system', rows: ['Mounted by many instances', 'Regional, spans AZs', 'Shared files, web content'], accent: false },
  { x: 440, label: 'Object: S3', sub: 'objects behind an API', rows: ['Accessed through an API', 'Regional, no capacity planning', 'Static files, backups, data lakes'], accent: true },
]

export function StorageTypesBody() {
  return (
    <>
      {columns.map((c) => (
        <g key={c.label}>
          <Box x={c.x} y={15} w={190} h={60} label={c.label} sub={c.sub} accent={c.accent} />
          {c.rows.map((r, i) => (
            <Note key={r} x={c.x + 8} y={105 + i * 22}>
              {r}
            </Note>
          ))}
        </g>
      ))}
      <Note x={10} y={185}>Exception: EBS Multi-Attach shares one io1 or io2 volume (io2 only on Windows)</Note>
      <Note x={10} y={203}>with up to 16 Nitro instances in a single AZ.</Note>
    </>
  )
}

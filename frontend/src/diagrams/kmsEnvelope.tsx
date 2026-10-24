import { Arrow, Badge, Box, Steps } from './parts'

export function KmsEnvelopeBody() {
  return (
    <>
      <Box x={10} y={70} w={140} h={60} label="Application" />
      <Box x={250} y={15} w={150} h={60} label="AWS KMS" sub="KMS key stays in KMS" accent />
      <Box x={470} y={70} w={160} h={60} label="Storage" sub="S3, EBS, a database" />
      <Arrow from={[150, 76]} to={[250, 38]} />
      <Badge x={190} y={42} n={1} />
      <Arrow from={[250, 62]} to={[150, 102]} dashed />
      <Badge x={212} y={95} n={2} />
      <Arrow from={[150, 118]} to={[470, 118]} />
      <Badge x={310} y={106} n={4} />
      <Badge x={80} y={62} n={3} />
      <Badge x={80} y={142} n={5} />
      <Steps
        x={10}
        y={168}
        items={[
          'The app asks KMS for a data key (GenerateDataKey)',
          'KMS returns it twice: in plaintext, and encrypted under the KMS key',
          'The app encrypts the data locally with the plaintext key, then discards it',
          'The app stores the encrypted data next to the encrypted data key',
          'To read: KMS decrypts the data key (Decrypt) and the app decrypts the data',
        ]}
      />
    </>
  )
}

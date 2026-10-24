import { Arrow, Badge, Box, Group, Steps } from './parts'

export function IamCrossAccountBody() {
  return (
    <>
      <Group x={10} y={10} w={210} h={190} label="Account A (the caller)" />
      <Group x={420} y={10} w={210} h={190} label="Account B (owns the resources)" />
      <Box x={25} y={75} w={180} h={60} label="User or role" sub="allowed to call AssumeRole" />
      <Box x={265} y={75} w={110} h={60} label="AWS STS" sub="issues credentials" accent />
      <Box x={435} y={40} w={180} h={50} label="IAM role" sub="trust policy trusts Account A" />
      <Box x={435} y={140} w={180} h={44} label="Resources" />
      <Arrow from={[205, 95]} to={[265, 95]} />
      <Badge x={235} y={80} n={1} />
      <Arrow from={[375, 95]} to={[435, 68]} />
      <Badge x={400} y={72} n={2} />
      <Arrow from={[265, 120]} to={[205, 120]} dashed />
      <Badge x={235} y={138} n={3} />
      <Arrow from={[525, 90]} to={[525, 140]} />
      <Badge x={548} y={115} n={4} />
      <Steps
        x={10}
        y={228}
        items={[
          'The caller in Account A requests sts:AssumeRole for the role in Account B',
          'It works only if A allows the call and the role trust policy trusts A',
          'STS returns temporary credentials for the role',
          'The caller then acts with the role permissions on Account B resources',
        ]}
      />
    </>
  )
}

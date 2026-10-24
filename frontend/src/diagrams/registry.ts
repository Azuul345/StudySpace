import { VpcPublicPrivateBody } from './vpcPublicPrivate'
import { SqsDecouplingBody } from './sqsDecoupling'
import { S3StorageLifecycleBody } from './s3StorageLifecycle'
import type { DiagramDef } from './types'

/// Diagram ids that lessons may list. Unknown ids are ignored, so an older app never breaks on a newer pack.
export const diagramRegistry: Record<string, DiagramDef> = {
  'vpc-public-private': {
    title: 'Public and private subnets with a NAT gateway',
    description:
      'Internet traffic enters through the internet gateway to a load balancer in the public subnet, which forwards it to EC2 instances in the private subnet. The private instances start outbound connections through a NAT gateway in the public subnet, so the internet cannot start connections to them. Simplified: one Availability Zone is shown, while real designs use a NAT gateway and load balancer subnet in each of at least two.',
    viewBox: '0 0 640 365',
    Body: VpcPublicPrivateBody,
  },
  'sqs-decoupling': {
    title: 'Decoupling a web tier from workers with SQS',
    description:
      'The web tier sends messages to an SQS queue instead of calling the workers directly. Workers pull messages from the queue and process them at their own pace, so a traffic spike builds up in the queue rather than overloading them. Auto Scaling can add workers as the queue backlog per instance grows.',
    viewBox: '0 0 640 215',
    Body: SqsDecouplingBody,
  },
  's3-storage-lifecycle': {
    title: 'S3 storage classes and lifecycle transitions',
    description:
      'A lifecycle rule can move objects from S3 Standard to S3 Standard-IA and then to the Glacier archive classes as they age. Each step down lowers the storage price per GB but usually makes retrieval slower and adds per-GB retrieval fees, though Glacier Flexible Retrieval Bulk retrievals are free. A lifecycle rule cannot move objects back up the chain. This is simplified: S3 has more classes, such as Intelligent-Tiering and Glacier Instant Retrieval.',
    viewBox: '0 0 640 215',
    Body: S3StorageLifecycleBody,
  },
}

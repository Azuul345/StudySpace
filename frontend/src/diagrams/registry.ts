import { IamCrossAccountBody } from './iamCrossAccount'
import { KmsEnvelopeBody } from './kmsEnvelope'
import { MultiAzWebBody } from './multiAzWeb'
import { StorageTypesBody } from './storageTypes'
import { AutoScalingTargetTrackingBody } from './autoScalingTargetTracking'
import { CacheAndReplicasBody } from './cacheAndReplicas'
import { CloudfrontEdgeBody } from './cloudfrontEdge'
import { StreamingIngestionBody } from './streamingIngestion'
import { Ec2PurchaseOptionsBody } from './ec2PurchaseOptions'
import { DynamoCapacityModesBody } from './dynamoCapacityModes'
import { EndpointVsNatBody } from './endpointVsNat'
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
  'iam-cross-account-role': {
    title: 'Cross-account access with an IAM role',
    description:
      'A user or role in Account A calls sts:AssumeRole for a role in Account B. It works only if Account A allows the call in an identity policy and the role trust policy in Account B trusts Account A. STS returns temporary credentials, and the caller then acts with the role permissions on Account B resources.',
    viewBox: '0 0 640 310',
    Body: IamCrossAccountBody,
  },
  'kms-envelope-encryption': {
    title: 'Envelope encryption with AWS KMS',
    description:
      'The application asks KMS for a data key and receives it twice: in plaintext and encrypted under a symmetric KMS key. The application encrypts the data locally with the plaintext key, discards that key, and stores the encrypted data next to the encrypted data key. To read the data, KMS decrypts the data key and the application decrypts the data.',
    viewBox: '0 0 640 280',
    Body: KmsEnvelopeBody,
  },
  'multi-az-web-tier': {
    title: 'A highly available web tier across two Availability Zones',
    description:
      'A load balancer spreads requests across web servers in an Auto Scaling group that spans two Availability Zones. The RDS primary replicates synchronously to a standby in the other zone, and RDS fails over to the standby automatically, after a short interruption, if the primary or its zone fails. Simplified: all web servers use the primary, and a Multi-AZ standby does not serve reads in this setup.',
    viewBox: '0 0 640 305',
    Body: MultiAzWebBody,
  },
  'storage-types': {
    title: 'Block, file and object storage compared',
    description:
      'Block storage (EBS) is a volume attached to an instance in one Availability Zone. File storage (EFS) is a shared file system that many instances across zones mount. Object storage (S3) is reached through an API and needs no capacity planning. EBS Multi-Attach is the exception for sharing a volume: io1 and io2 volumes (io2 only on Windows), up to 16 Nitro instances, one zone. Regional EFS file systems span zones.',
    viewBox: '0 0 640 220',
    Body: StorageTypesBody,
  },
  'auto-scaling-target-tracking': {
    title: 'Auto Scaling with a target tracking policy',
    description:
      'Amazon EC2 sends metrics for the instances in an Auto Scaling group to CloudWatch. A target tracking policy keeps a chosen metric, such as average CPU utilization, near a target value by adding instances when it rises above the target and removing them when it falls below. Auto Scaling creates and manages the CloudWatch alarms for the policy, so you do not create them. A load balancer spreads traffic across the instances.',
    viewBox: '0 0 640 220',
    Body: AutoScalingTargetTrackingBody,
  },
  'cache-and-read-replicas': {
    title: 'Speeding up reads with a cache and read replicas',
    description:
      'The application checks an ElastiCache cache first, which answers hot reads from memory. On a miss, reads go to read replicas, which scale read traffic. All writes go to the primary database, which replicates to the replicas asynchronously, so replicas can lag slightly. A cache can also hold stale data until entries expire.',
    viewBox: '0 0 640 250',
    Body: CacheAndReplicasBody,
  },
  'cloudfront-edge-caching': {
    title: 'How CloudFront caches content at the edge',
    description:
      'A user request is routed to a nearby CloudFront edge location. On a cache hit the edge returns its cached copy immediately. On a cache miss the edge requests the object from the origin, often through a regional edge cache, where the origin can be an S3 bucket or a load balancer. CloudFront caches the response and returns it, so later users get it from the edge.',
    viewBox: '0 0 640 260',
    Body: CloudfrontEdgeBody,
  },
  'streaming-ingestion-pipeline': {
    title: 'A streaming ingestion pipeline into a data lake',
    description:
      'Producers such as applications and devices write records to Kinesis Data Streams. Custom consumers such as Lambda functions can process the stream, and Amazon Data Firehose can load it into Amazon S3 without custom code, in near real time. Athena then queries the data in S3 with SQL.',
    viewBox: '0 0 640 205',
    Body: StreamingIngestionBody,
  },
  'ec2-purchase-options': {
    title: 'Matching EC2 purchase options to the workload',
    description:
      'Cover a steady, always-on baseline with a Savings Plan or Reserved Instances in exchange for a one or three year commitment (AWS recommends Savings Plans over Reserved Instances). Use On-Demand for spiky or unpredictable demand with no commitment. Use Spot Instances for fault-tolerant, flexible work that can handle interruption, such as batch jobs.',
    viewBox: '0 0 640 270',
    Body: Ec2PurchaseOptionsBody,
  },
  'dynamodb-capacity-modes': {
    title: 'DynamoDB provisioned and on-demand capacity modes',
    description:
      'In provisioned mode you set read and write capacity, which can be cheaper for steady, predictable traffic when the capacity is well used, and can use auto scaling. In on-demand mode you pay per request and plan no capacity, which suits spiky or unknown traffic.',
    viewBox: '0 0 640 185',
    Body: DynamoCapacityModesBody,
  },
  'gateway-endpoint-vs-nat': {
    title: 'Reaching S3 from a private subnet: gateway endpoint or NAT gateway',
    description:
      'An instance in a private subnet can reach S3 through a NAT gateway and internet gateway, which adds an hourly charge and per-GB data processing charges. A gateway endpoint for S3 or DynamoDB lets the instance reach the service without a NAT gateway or internet gateway, and has no additional charge.',
    viewBox: '0 0 640 250',
    Body: EndpointVsNatBody,
  },
}

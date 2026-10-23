using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

/// Known-good pack files. Tests make one change at a time with Replace, so each failure points at one rule.
internal static class PackSamples
{
    public const string Manifest = """
{
  "schemaVersion": 1,
  "packId": "aws-saa-c03",
  "version": "2026.10.1",
  "exam": { "code": "SAA-C03", "name": "AWS Certified Solutions Architect - Associate" },
  "domains": [
    { "id": "secure", "name": "Design Secure Architectures", "weightPercent": 30, "sortOrder": 1 },
    { "id": "resilient", "name": "Design Resilient Architectures", "weightPercent": 70, "sortOrder": 2 }
  ],
  "sources": [ { "url": "https://aws.amazon.com/certification/certified-solutions-architect-associate/", "accessedOn": "2026-10-23" } ]
}
""";

    public const string Topic = """
{
  "schemaVersion": 1,
  "topicId": "s3-security",
  "domainId": "secure",
  "name": "S3 security",
  "lesson": {
    "summary": "Block Public Access and bucket policies control who can reach S3 data.",
    "keyPoints": [ "Block Public Access works at account and bucket level" ],
    "docLinks": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html" ]
  },
  "scenarios": [ { "id": "sc-1", "text": "A company stores logs in an S3 bucket." } ],
  "questions": [
    {
      "id": "s3sec-001", "type": "mcq", "difficulty": 2, "tags": [ "S3" ],
      "prompt": "Which setting blocks all public access at the account level?",
      "options": [
        { "id": "a", "text": "Bucket ACL", "correct": false },
        { "id": "b", "text": "Block Public Access", "correct": true }
      ],
      "explanation": "B is correct. A bucket ACL works per bucket only.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html" ]
    },
    {
      "id": "s3sec-002", "type": "multi", "difficulty": 3, "tags": [ "S3", "KMS" ],
      "prompt": "Which two options encrypt S3 objects at rest? (Select two.)",
      "options": [
        { "id": "a", "text": "SSE-S3", "correct": true },
        { "id": "b", "text": "SSL certificate", "correct": false },
        { "id": "c", "text": "SSE-KMS", "correct": true }
      ],
      "explanation": "A and C are correct. B secures data in transit, not at rest.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/serv-side-encryption.html" ]
    },
    {
      "id": "s3sec-003", "type": "scenario", "difficulty": 2, "tags": [ "S3" ],
      "scenarioId": "sc-1",
      "prompt": "Which option keeps the log bucket private?",
      "options": [
        { "id": "a", "text": "Block Public Access", "correct": true },
        { "id": "b", "text": "Public read ACL", "correct": false }
      ],
      "explanation": "A is correct. B exposes the logs.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html" ]
    },
    {
      "id": "s3sec-004", "type": "matching", "difficulty": 1, "tags": [ "IAM" ],
      "prompt": "Match each service to its purpose.",
      "pairs": [
        { "left": "KMS", "right": "Manage encryption keys" },
        { "left": "WAF", "right": "Filter web requests" }
      ],
      "explanation": "KMS manages keys. WAF filters HTTP traffic.",
      "sources": [ "https://docs.aws.amazon.com/kms/latest/developerguide/overview.html" ]
    },
    {
      "id": "s3sec-005", "type": "truefalse", "difficulty": 1, "tags": [ "S3" ],
      "statement": "S3 bucket names are globally unique.",
      "answer": true,
      "explanation": "True. Bucket names must be unique across all AWS accounts.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucketnamingrules.html" ]
    },
    {
      "id": "s3sec-006", "type": "ordering", "difficulty": 2, "tags": [ "S3" ],
      "prompt": "Put these steps in order.",
      "steps": [ "Create bucket", "Enable encryption", "Upload object" ],
      "correctOrder": [ 0, 1, 2 ],
      "explanation": "Create first, then enable encryption, then upload.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/create-bucket-overview.html" ]
    },
    {
      "id": "s3sec-007", "type": "typed", "difficulty": 3, "tags": [ "S3" ],
      "prompt": "Name one way to encrypt S3 objects at rest.",
      "rubric": [ { "any": [ "encrypt", "encryption", "sse" ] } ],
      "explanation": "Server-side encryption (SSE) encrypts objects at rest.",
      "sources": [ "https://docs.aws.amazon.com/AmazonS3/latest/userguide/serv-side-encryption.html" ]
    }
  ]
}
""";

    public const string Flashcards = """
{
  "schemaVersion": 1,
  "cards": [ { "id": "fc-001", "term": "KMS", "definition": "AWS key management service.", "sources": [ "https://docs.aws.amazon.com/kms/latest/developerguide/overview.html" ] } ]
}
""";

    public static List<PackFile> ValidPack() =>
    [
        new("pack.json", Manifest),
        new("topics/s3-security.json", Topic),
        new("flashcards.json", Flashcards),
    ];

    /// Fails loudly if the snippet is missing, so a test cannot pass by editing nothing.
    public static string Replace(string text, string from, string to)
    {
        Assert.Contains(from, text);
        return text.Replace(from, to, StringComparison.Ordinal);
    }
}

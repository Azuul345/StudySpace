namespace StudySpace.Infrastructure.Tests;

internal static class SamplePack
{
    public const string Manifest = """
{
  "schemaVersion": 1,
  "packId": "test-pack",
  "version": "2026.10.1",
  "exam": { "code": "TEST-1", "name": "Test exam" },
  "domains": [ { "id": "d1", "name": "Domain one", "weightPercent": 100, "sortOrder": 1 } ],
  "sources": [ { "url": "https://example.com/guide", "accessedOn": "2026-10-23" } ]
}
""";

    public const string Topic = """
{
  "schemaVersion": 1,
  "topicId": "t1",
  "domainId": "d1",
  "name": "Topic one",
  "lesson": { "summary": "Short summary.", "keyPoints": [ "A point" ], "docLinks": [ "https://example.com/doc" ] },
  "scenarios": [],
  "questions": [
    {
      "id": "q-001", "type": "truefalse", "difficulty": 1, "tags": [],
      "statement": "Water is wet.", "answer": true,
      "explanation": "True.", "sources": [ "https://example.com/src" ]
    }
  ]
}
""";
}

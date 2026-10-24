using StudySpace.Application.ContentPacks;

namespace StudySpace.Api.Tests.Support;

/// A small verified pack: two domains (30/70), two topics, one question of every type.
internal static class ApiSamples
{
    public static List<PackFile> VerifiedPack(string version = "2026.10.1", string status = "verified") =>
    [
        new("pack.json", Manifest("api-test", version, "API-1", "API exam")),
        new("topics/secure.json", SecureTopic),
        new("topics/resilient.json", ResilientTopic),
        new("flashcards.json", Flashcards),
        new("verification.json", "{\"status\":\"" + status + "\",\"items\":[{\"kind\":\"question\",\"id\":\"q-mcq\",\"result\":\"pass\",\"caveat\":\"Page does not state the limit.\"}]}"),
    ];

    public static List<PackFile> PendingPack() =>
    [
        new("pack.json", Manifest("other-pack", "2026.10.1", "OTHER-1", "Other exam")),
        new("topics/secure.json", SecureTopic.Replace("\"s3-sec\"", "\"o-sec\"", StringComparison.Ordinal).Replace("\"q-", "\"o-q-", StringComparison.Ordinal)),
        new("topics/resilient.json", ResilientTopic.Replace("\"ha\"", "\"o-ha\"", StringComparison.Ordinal).Replace("\"q-", "\"o-q-", StringComparison.Ordinal)),
    ];

    private static string Manifest(string packId, string version, string code, string name) => $$"""
{
  "schemaVersion": 1, "packId": "{{packId}}", "version": "{{version}}",
  "exam": { "code": "{{code}}", "name": "{{name}}" },
  "domains": [
    { "id": "secure", "name": "Secure", "weightPercent": 30, "sortOrder": 1 },
    { "id": "resilient", "name": "Resilient", "weightPercent": 70, "sortOrder": 2 }
  ],
  "sources": [ { "url": "https://example.com/guide", "accessedOn": "2026-10-23" } ]
}
""";

    private const string SecureTopic = """
{
  "schemaVersion": 1, "topicId": "s3-sec", "domainId": "secure", "name": "S3 security",
  "lesson": { "summary": "Summary one.", "keyPoints": [ "Point" ], "docLinks": [ "https://example.com/doc" ] },
  "scenarios": [ { "id": "sc-1", "text": "A company stores logs." } ],
  "questions": [
    { "id": "q-mcq", "type": "mcq", "difficulty": 1, "tags": [], "prompt": "Pick b.",
      "options": [ { "id": "a", "text": "A", "correct": false }, { "id": "b", "text": "B", "correct": true } ],
      "explanation": "B is right.", "sources": [ "https://example.com/s" ] },
    { "id": "q-multi", "type": "multi", "difficulty": 2, "tags": [], "prompt": "Pick b and c.",
      "options": [ { "id": "a", "text": "A", "correct": false }, { "id": "b", "text": "B", "correct": true }, { "id": "c", "text": "C", "correct": true } ],
      "explanation": "B and C.", "sources": [ "https://example.com/s" ] },
    { "id": "q-scn1", "type": "scenario", "difficulty": 2, "tags": [], "scenarioId": "sc-1", "prompt": "Scenario one?",
      "options": [ { "id": "a", "text": "A", "correct": true }, { "id": "b", "text": "B", "correct": false } ],
      "explanation": "A.", "sources": [ "https://example.com/s" ] },
    { "id": "q-scn2", "type": "scenario", "difficulty": 2, "tags": [], "scenarioId": "sc-1", "prompt": "Scenario two?",
      "options": [ { "id": "a", "text": "A", "correct": false }, { "id": "b", "text": "B", "correct": true } ],
      "explanation": "B.", "sources": [ "https://example.com/s" ] },
    { "id": "q-tf", "type": "truefalse", "difficulty": 1, "tags": [], "statement": "Water is wet.", "answer": true,
      "explanation": "True.", "sources": [ "https://example.com/s" ] },
    { "id": "q-order", "type": "ordering", "difficulty": 2, "tags": [], "prompt": "Order.",
      "steps": [ "x", "y", "z" ], "correctOrder": [2, 0, 1], "explanation": "Z then X then Y.", "sources": [ "https://example.com/s" ] },
    { "id": "q-match", "type": "matching", "difficulty": 2, "tags": [], "prompt": "Match.",
      "pairs": [ { "left": "KMS", "right": "Keys" }, { "left": "WAF", "right": "Filter" } ],
      "explanation": "KMS keys, WAF filter.", "sources": [ "https://example.com/s" ] },
    { "id": "q-typed", "type": "typed", "difficulty": 3, "tags": [], "prompt": "Name an encryption service.",
      "rubric": [ { "any": [ "kms", "sse" ] } ], "explanation": "KMS or SSE.", "sources": [ "https://example.com/s" ] }
  ]
}
""";

    private const string ResilientTopic = """
{
  "schemaVersion": 1, "topicId": "ha", "domainId": "resilient", "name": "High availability",
  "lesson": { "summary": "Summary two.", "keyPoints": [ "Multi-AZ" ], "docLinks": [ "https://example.com/ha" ] },
  "scenarios": [],
  "questions": [
    { "id": "q-ha1", "type": "mcq", "difficulty": 1, "tags": [], "prompt": "HA one?",
      "options": [ { "id": "a", "text": "A", "correct": true }, { "id": "b", "text": "B", "correct": false } ],
      "explanation": "A.", "sources": [ "https://example.com/s" ] },
    { "id": "q-ha2", "type": "mcq", "difficulty": 1, "tags": [], "prompt": "HA two?",
      "options": [ { "id": "a", "text": "A", "correct": true }, { "id": "b", "text": "B", "correct": false } ],
      "explanation": "A.", "sources": [ "https://example.com/s" ] }
  ]
}
""";

    private const string Flashcards = """
{ "schemaVersion": 1, "cards": [
  { "id": "fc-1", "term": "KMS", "definition": "Key service.", "sources": [ "https://example.com/s" ] },
  { "id": "fc-2", "term": "WAF", "definition": "Web filter.", "sources": [ "https://example.com/s" ] }
] }
""";
}

namespace StudySpace.Application.ContentPacks;

public enum VerificationStatus { Pending, Verified, Failed }

/// Caveats map a question id to what the verifier could not fully confirm. A question without an entry was fully verified.
public sealed record ParsedPack(
    string PackId,
    string Version,
    string ExamCode,
    string ExamName,
    VerificationStatus Status,
    IReadOnlyList<ParsedDomain> Domains,
    IReadOnlyList<ParsedTopic> Topics,
    IReadOnlyList<ParsedFlashcard> Flashcards,
    IReadOnlyDictionary<string, string>? Caveats = null);

public sealed record ParsedDomain(string Id, string Name, int WeightPercent, int SortOrder);

public sealed record ParsedTopic(
    string Id,
    string DomainId,
    string Name,
    int SortOrder,
    string Summary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> DocLinks,
    IReadOnlyList<ParsedScenario> Scenarios,
    IReadOnlyList<ParsedQuestion> Questions,
    IReadOnlyList<string>? Diagrams = null);

public sealed record ParsedScenario(string Id, string Text);

public sealed record ParsedOption(string Id, string Text, bool IsCorrect);

/// Options hold mcq, multi and scenario answers. DataJson holds the validated type-specific payload
/// for matching (pairs), ordering (steps and correctOrder), truefalse (answer) and typed (rubric).
public sealed record ParsedQuestion(
    string Id,
    string Type,
    string Prompt,
    string Explanation,
    int Difficulty,
    string? ScenarioId,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Sources,
    IReadOnlyList<ParsedOption> Options,
    string? DataJson);

public sealed record ParsedFlashcard(string Id, string Term, string Definition);

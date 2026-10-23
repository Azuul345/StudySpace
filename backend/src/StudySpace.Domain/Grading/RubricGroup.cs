namespace StudySpace.Domain.Grading;

/// One required idea in a typed answer. Any one of the accepted terms satisfies the group.
public sealed record RubricGroup(IReadOnlyList<string> AcceptedTerms);

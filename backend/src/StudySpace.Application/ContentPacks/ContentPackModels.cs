namespace StudySpace.Application.ContentPacks;

/// One file from a content pack, with its path relative to the pack root.
public sealed record PackFile(string Name, string Content);

/// A single problem found in a pack file. Path is the JSON path inside the file, e.g. questions[0].options.
public sealed record ValidationError(string File, string Path, string Message);

public sealed record ValidationResult(IReadOnlyList<ValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

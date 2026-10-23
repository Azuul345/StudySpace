namespace StudySpace.Application.ContentPacks;

/// Pack file names must stay inside the pack folder (REQ-SEC-002).
/// Rejects absolute paths, drive letters, backslashes, empty segments, and ".." segments.
public static class PackPathGuard
{
    private const char Backslash = (char)92;

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/'))
        {
            return false;
        }

        if (path.Contains('\0') || path.Contains(Backslash) || path.Contains(':'))
        {
            return false;
        }

        return path.Split('/').All(segment => segment is not ("" or "." or ".."));
    }
}

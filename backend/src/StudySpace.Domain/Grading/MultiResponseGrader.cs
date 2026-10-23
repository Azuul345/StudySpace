namespace StudySpace.Domain.Grading;

/// Multiple response (select all that apply) is correct only when the selected options
/// exactly match the correct options. Partial credit is not used (REQ-Q-002).
public static class MultiResponseGrader
{
    public static bool IsCorrect(IEnumerable<string> selected, IEnumerable<string> correct) =>
        new HashSet<string>(selected, StringComparer.Ordinal).SetEquals(correct);
}

using System.Text;

namespace StudySpace.Domain.Grading;

/// Typed answers are correct when every rubric group has at least one accepted term
/// present as whole words in the answer. Case and punctuation are ignored (REQ-Q-007).
public static class TypedAnswerGrader
{
    public static bool IsCorrect(string answer, IReadOnlyList<RubricGroup> rubric)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(rubric);

        if (rubric.Count == 0)
        {
            throw new ArgumentException("A rubric needs at least one group.", nameof(rubric));
        }

        var normalized = Normalize(answer);
        if (normalized.Length == 0)
        {
            return false;
        }

        // Pad with spaces so a term only matches as whole words.
        var padded = $" {normalized} ";
        return rubric.All(group =>
            group.AcceptedTerms.Any(term => padded.Contains($" {Normalize(term)} ", StringComparison.Ordinal)));
    }

    /// Lower-case letters and digits, with every other run of characters becoming one space.
    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(ch));
                pendingSpace = false;
            }
            else
            {
                pendingSpace = true;
            }
        }

        return builder.ToString();
    }
}

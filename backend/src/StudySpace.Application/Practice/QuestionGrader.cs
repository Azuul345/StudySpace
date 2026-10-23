using System.Text.Json;
using StudySpace.Domain.Grading;

namespace StudySpace.Application.Practice;

public sealed record GradableOption(string Id, bool IsCorrect);

/// DataJson is the type-specific payload stored at import: pairs (matching), steps and correctOrder (ordering),
/// answer (truefalse), or the rubric array (typed).
public sealed record GradableQuestion(string Type, IReadOnlyList<GradableOption> Options, string? DataJson);

public sealed record GradeOutcome(bool Valid, bool IsCorrect, bool NeedsConfirmation, string? Error)
{
    public static GradeOutcome Invalid(string error) => new(false, false, false, error);
    public static GradeOutcome Result(bool correct, bool needsConfirmation = false) => new(true, correct, needsConfirmation, null);
}

/// Grades one answer on the server. The answer comes from the client, so its shape is checked before it is used.
/// Answer shapes: mcq, multi, scenario: {"selected":["id"]}; truefalse: {"value":true}; ordering: {"order":[2,0,1]};
/// matching: {"pairs":[{"left":"","right":""}]}; typed: {"text":""}.
public static class QuestionGrader
{
    public const int MaxTypedLength = 2000;

    public static GradeOutcome Grade(GradableQuestion question, JsonElement answer)
    {
        if (answer.ValueKind != JsonValueKind.Object)
        {
            return GradeOutcome.Invalid("Answer must be an object.");
        }

        return question.Type switch
        {
            "mcq" => GradeSelection(question, answer, single: true),
            "multi" or "scenario" => GradeSelection(question, answer, single: false),
            "truefalse" => GradeTrueFalse(question, answer),
            "ordering" => GradeOrdering(question, answer),
            "matching" => GradeMatching(question, answer),
            "typed" => GradeTyped(question, answer),
            _ => GradeOutcome.Invalid("Unknown question type."),
        };
    }

    private static GradeOutcome GradeSelection(GradableQuestion question, JsonElement answer, bool single)
    {
        if (!answer.TryGetProperty("selected", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return GradeOutcome.Invalid("Answer needs a 'selected' list.");
        }

        var selected = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                return GradeOutcome.Invalid("Selected options must be text ids.");
            }

            selected.Add(item.GetString()!);
        }

        var known = question.Options.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        if (selected.Count == 0 || selected.Distinct(StringComparer.Ordinal).Count() != selected.Count || selected.Any(id => !known.Contains(id)))
        {
            return GradeOutcome.Invalid("Select one or more distinct, known options.");
        }

        if (single && selected.Count != 1)
        {
            return GradeOutcome.Invalid("Select exactly one option.");
        }

        var correct = question.Options.Where(o => o.IsCorrect).Select(o => o.Id);
        return GradeOutcome.Result(MultiResponseGrader.IsCorrect(selected, correct));
    }

    private static GradeOutcome GradeTrueFalse(GradableQuestion question, JsonElement answer)
    {
        if (!answer.TryGetProperty("value", out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return GradeOutcome.Invalid("Answer needs a true or false 'value'.");
        }

        using var data = JsonDocument.Parse(question.DataJson!);
        return GradeOutcome.Result(data.RootElement.GetProperty("answer").GetBoolean() == value.GetBoolean());
    }

    private static GradeOutcome GradeOrdering(GradableQuestion question, JsonElement answer)
    {
        using var data = JsonDocument.Parse(question.DataJson!);
        var expected = data.RootElement.GetProperty("correctOrder").EnumerateArray().Select(e => e.GetInt32()).ToList();

        if (!answer.TryGetProperty("order", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return GradeOutcome.Invalid("Answer needs an 'order' list.");
        }

        var given = new List<int>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var index))
            {
                return GradeOutcome.Invalid("Order entries must be whole numbers.");
            }

            given.Add(index);
        }

        var isPermutation = given.Count == expected.Count && given.Distinct().Count() == given.Count && given.All(i => i >= 0 && i < expected.Count);
        return isPermutation ? GradeOutcome.Result(given.SequenceEqual(expected)) : GradeOutcome.Invalid("Order must use every step once.");
    }

    private static GradeOutcome GradeMatching(GradableQuestion question, JsonElement answer)
    {
        using var data = JsonDocument.Parse(question.DataJson!);
        var expected = data.RootElement.EnumerateArray().Select(p => PairKey(p.GetProperty("left").GetString()!, p.GetProperty("right").GetString()!)).ToHashSet(StringComparer.Ordinal);

        if (!answer.TryGetProperty("pairs", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return GradeOutcome.Invalid("Answer needs a 'pairs' list.");
        }

        var given = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("left", out var left) || left.ValueKind != JsonValueKind.String
                || !item.TryGetProperty("right", out var right) || right.ValueKind != JsonValueKind.String)
            {
                return GradeOutcome.Invalid("Each pair needs text 'left' and 'right'.");
            }

            count++;
            given.Add(PairKey(left.GetString()!, right.GetString()!));
        }

        if (count != expected.Count || given.Count != count)
        {
            return GradeOutcome.Invalid("Give every pair exactly once.");
        }

        return GradeOutcome.Result(given.SetEquals(expected));
    }

    private static GradeOutcome GradeTyped(GradableQuestion question, JsonElement answer)
    {
        if (!answer.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(text.GetString()))
        {
            return GradeOutcome.Invalid("Answer needs some 'text'.");
        }

        if (text.GetString()!.Length > MaxTypedLength)
        {
            return GradeOutcome.Invalid($"Answer is longer than {MaxTypedLength} characters.");
        }

        using var data = JsonDocument.Parse(question.DataJson!);
        var rubric = data.RootElement.EnumerateArray()
            .Select(g => new RubricGroup(g.GetProperty("any").EnumerateArray().Select(t => t.GetString()!).ToList()))
            .ToList();

        return GradeOutcome.Result(TypedAnswerGrader.IsCorrect(text.GetString()!, rubric), needsConfirmation: true);
    }

    private static string PairKey(string left, string right) => left + "\u0000" + right;
}

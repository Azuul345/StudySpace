using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

public sealed record OptionView(string Id, string Text);

public sealed record PairsView(IReadOnlyList<string> Lefts, IReadOnlyList<string> Rights);

public sealed record ScenarioView(int Id, string Text);

/// What the client sees while answering. It never carries correct flags, explanations, or the answer data.
public sealed record QuestionView(
    int Id,
    string Type,
    string Prompt,
    int Difficulty,
    IReadOnlyList<string> Tags,
    int TopicId,
    ScenarioView? Scenario,
    IReadOnlyList<OptionView>? Options,
    PairsView? Pairs,
    IReadOnlyList<string>? Steps);

public static class QuestionViews
{
    public static QuestionView ForAnswering(QuestionEntity q, ScenarioEntity? scenario)
    {
        IReadOnlyList<OptionView>? options = q.Options.Count > 0
            ? q.Options.OrderBy(o => o.SortOrder).Select(o => new OptionView(o.Slug, o.Text)).ToList()
            : null;

        PairsView? pairs = null;
        IReadOnlyList<string>? steps = null;
        if (q.Type == "matching")
        {
            using var doc = JsonDocument.Parse(q.DataJson!);
            var items = doc.RootElement.EnumerateArray().Select(p => (Left: p.GetProperty("left").GetString()!, Right: p.GetProperty("right").GetString()!)).ToList();
            pairs = new PairsView(items.Select(p => p.Left).ToList(), items.Select(p => p.Right).OrderBy(r => Scramble(q.Id, r), StringComparer.Ordinal).ToList());
        }
        else if (q.Type == "ordering")
        {
            using var doc = JsonDocument.Parse(q.DataJson!);
            steps = doc.RootElement.GetProperty("steps").EnumerateArray().Select(s => s.GetString()!).ToList();
        }

        return new QuestionView(
            q.Id, q.Type, q.Prompt, q.Difficulty,
            JsonSerializer.Deserialize<List<string>>(q.TagsJson) ?? [],
            q.TopicId,
            scenario is null ? null : new ScenarioView(scenario.Id, scenario.Text),
            options, pairs, steps);
    }

    /// The right-hand side of a matching question is shown in a stable order that does not follow the answer order.
    private static string Scramble(int questionId, string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{questionId}:{text}")));

    /// The right answer as plain text, for the audit list.
    public static string AnswerText(QuestionEntity q)
    {
        switch (q.Type)
        {
            case "mcq" or "multi" or "scenario":
                return string.Join("; ", q.Options.Where(o => o.IsCorrect).OrderBy(o => o.SortOrder).Select(o => $"{o.Slug}) {o.Text}"));
            case "truefalse":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return doc.RootElement.GetProperty("answer").GetBoolean() ? "True" : "False";
                }

            case "ordering":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    var steps = doc.RootElement.GetProperty("steps").EnumerateArray().Select(s => s.GetString()!).ToList();
                    return string.Join(" → ", doc.RootElement.GetProperty("correctOrder").EnumerateArray().Select(e => steps[e.GetInt32()]));
                }

            case "matching":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return string.Join("; ", doc.RootElement.EnumerateArray().Select(p => $"{p.GetProperty("left").GetString()} → {p.GetProperty("right").GetString()}"));
                }

            default:
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return string.Join("; ", doc.RootElement.EnumerateArray().Select(g => string.Join(" / ", g.GetProperty("any").EnumerateArray().Select(t => t.GetString()))));
                }
        }
    }

    /// What the client sees after answering: the right answer in the same shape as an answer.
    public static object CorrectAnswer(QuestionEntity q)
    {
        switch (q.Type)
        {
            case "mcq" or "multi" or "scenario":
                return new { selected = q.Options.Where(o => o.IsCorrect).OrderBy(o => o.SortOrder).Select(o => o.Slug).ToList() };
            case "truefalse":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return new { value = doc.RootElement.GetProperty("answer").GetBoolean() };
                }

            case "ordering":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return new { order = doc.RootElement.GetProperty("correctOrder").EnumerateArray().Select(e => e.GetInt32()).ToList() };
                }

            case "matching":
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return new { pairs = doc.RootElement.EnumerateArray().Select(p => new { left = p.GetProperty("left").GetString(), right = p.GetProperty("right").GetString() }).ToList() };
                }

            default:
                using (var doc = JsonDocument.Parse(q.DataJson!))
                {
                    return new { keywords = doc.RootElement.EnumerateArray().Select(g => g.GetProperty("any").EnumerateArray().Select(t => t.GetString()).ToList()).ToList() };
                }
        }
    }
}

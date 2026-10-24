using System.Text.Json;

namespace StudySpace.Application.ContentPacks;

/// Reads a pack that already passed PackValidator. Call Validate first: this parser assumes the shape is right.
/// The verification report is the exception. It is read defensively, and anything unexpected means Pending.
public static class PackParser
{
    public static ParsedPack Parse(IReadOnlyList<PackFile> files)
    {
        using var manifestDoc = JsonDocument.Parse(files.First(f => f.Name == "pack.json").Content);
        var manifest = manifestDoc.RootElement;
        var exam = manifest.GetProperty("exam");

        var domains = manifest.GetProperty("domains").EnumerateArray()
            .Select(d => new ParsedDomain(Str(d, "id"), Str(d, "name"), d.GetProperty("weightPercent").GetInt32(), d.GetProperty("sortOrder").GetInt32()))
            .ToList();

        var topics = files
            .Where(f => f.Name.StartsWith("topics/", StringComparison.Ordinal))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select((f, index) => ParseTopic(f.Content, index + 1))
            .ToList();

        var flashcards = new List<ParsedFlashcard>();
        if (files.FirstOrDefault(f => f.Name == "flashcards.json") is { } cardFile)
        {
            using var cardDoc = JsonDocument.Parse(cardFile.Content);
            flashcards.AddRange(cardDoc.RootElement.GetProperty("cards").EnumerateArray()
                .Select(c => new ParsedFlashcard(Str(c, "id"), Str(c, "term"), Str(c, "definition"))));
        }

        var verification = files.FirstOrDefault(f => f.Name == "verification.json");

        return new ParsedPack(
            Str(manifest, "packId"), Str(manifest, "version"), Str(exam, "code"), Str(exam, "name"),
            ReadStatus(verification),
            domains, topics, flashcards, ReadCaveats(verification));
    }

    private static ParsedTopic ParseTopic(string json, int sortOrder)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var lesson = root.GetProperty("lesson");

        return new ParsedTopic(
            Str(root, "topicId"), Str(root, "domainId"), Str(root, "name"), sortOrder,
            Str(lesson, "summary"), Strings(lesson, "keyPoints"), Strings(lesson, "docLinks"),
            root.GetProperty("scenarios").EnumerateArray().Select(s => new ParsedScenario(Str(s, "id"), Str(s, "text"))).ToList(),
            root.GetProperty("questions").EnumerateArray().Select(ParseQuestion).ToList(),
            lesson.TryGetProperty("diagrams", out _) ? Strings(lesson, "diagrams") : []);
    }

    private static ParsedQuestion ParseQuestion(JsonElement q)
    {
        var type = Str(q, "type");
        var options = q.TryGetProperty("options", out var optionArray)
            ? optionArray.EnumerateArray().Select(o => new ParsedOption(Str(o, "id"), Str(o, "text"), o.GetProperty("correct").GetBoolean())).ToList()
            : [];

        string? data = type switch
        {
            "matching" => q.GetProperty("pairs").GetRawText(),
            "ordering" => $"{{\"steps\":{q.GetProperty("steps").GetRawText()},\"correctOrder\":{q.GetProperty("correctOrder").GetRawText()}}}",
            "truefalse" => q.GetProperty("answer").GetBoolean() ? "{\"answer\":true}" : "{\"answer\":false}",
            "typed" => q.GetProperty("rubric").GetRawText(),
            _ => null,
        };

        return new ParsedQuestion(
            Str(q, "id"), type, Str(q, type == "truefalse" ? "statement" : "prompt"), Str(q, "explanation"),
            q.GetProperty("difficulty").GetInt32(),
            q.TryGetProperty("scenarioId", out var scenario) ? scenario.GetString() : null,
            Strings(q, "tags"), Strings(q, "sources"), options, data);
    }

    private static VerificationStatus ReadStatus(PackFile? file)
    {
        if (file is null)
        {
            return VerificationStatus.Pending;
        }

        try
        {
            using var doc = JsonDocument.Parse(file.Content);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("status", out var status)
                && status.ValueKind == JsonValueKind.String)
            {
                return status.GetString() switch
                {
                    "verified" => VerificationStatus.Verified,
                    "failed" => VerificationStatus.Failed,
                    _ => VerificationStatus.Pending,
                };
            }
        }
        catch (JsonException)
        {
        }

        return VerificationStatus.Pending;
    }

    /// Question items that carry a non-empty caveat. Anything malformed is ignored, like the status.
    private static Dictionary<string, string> ReadCaveats(PackFile? file)
    {
        var caveats = new Dictionary<string, string>();
        if (file is null)
        {
            return caveats;
        }

        try
        {
            using var doc = JsonDocument.Parse(file.Content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                return caveats;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object
                    && Text(item, "kind") == "question"
                    && Text(item, "id") is { Length: > 0 } id
                    && Text(item, "caveat")?.Trim() is { Length: > 0 } caveat)
                {
                    caveats[id] = caveat;
                }
            }
        }
        catch (JsonException)
        {
        }

        return caveats;
    }

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()!;

    private static List<string> Strings(JsonElement e, string name) =>
        e.GetProperty(name).EnumerateArray().Select(x => x.GetString()!).ToList();
}

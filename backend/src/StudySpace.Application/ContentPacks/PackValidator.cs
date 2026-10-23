using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StudySpace.Application.ContentPacks;

/// Validates a content pack before import (REQ-IMP-001, REQ-SEC-002, docs/03-CONTENT-PACK-FORMAT.md).
/// Content is untrusted: unknown properties, wrong types, markup in text, unsafe paths, and oversized files are rejected.
public static class PackValidator
{
    public const int MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxPackBytes = 20 * 1024 * 1024;

    private const string ManifestName = "pack.json";
    private const string FlashcardsName = "flashcards.json";
    private const string VerificationName = "verification.json";
    private const string TopicPrefix = "topics/";

    private static readonly Regex SlugPattern = new("^[a-z0-9][a-z0-9-]{1,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex OptionIdPattern = new("^[a-z0-9][a-z0-9-]{0,15}$", RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new(@"^\d{4}\.\d{2}\.\d+$", RegexOptions.CultureInvariant);
    private static readonly Regex DatePattern = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant);

    private static readonly string[] ManifestFields = ["schemaVersion", "packId", "version", "exam", "domains", "sources"];
    private static readonly string[] TopicFields = ["schemaVersion", "topicId", "domainId", "name", "lesson", "scenarios", "questions"];
    private static readonly string[] FlashcardFileFields = ["schemaVersion", "cards"];
    private static readonly string[] QuestionCommonFields = ["id", "type", "difficulty", "tags", "explanation", "sources"];

    private static readonly Dictionary<string, string[]> TypeFields = new()
    {
        ["mcq"] = ["prompt", "options"],
        ["multi"] = ["prompt", "options"],
        ["scenario"] = ["prompt", "options", "scenarioId"],
        ["matching"] = ["prompt", "pairs"],
        ["truefalse"] = ["statement", "answer"],
        ["ordering"] = ["prompt", "steps", "correctOrder"],
        ["typed"] = ["prompt", "rubric"],
    };

    public static ValidationResult Validate(IReadOnlyList<PackFile> files)
    {
        var errors = new List<ValidationError>();
        var safeFiles = new List<PackFile>();
        var totalBytes = 0L;

        foreach (var file in files)
        {
            if (!PackPathGuard.IsSafeRelativePath(file.Name))
            {
                errors.Add(new ValidationError(file.Name, string.Empty, "Unsafe file path."));
                continue;
            }

            var bytes = Encoding.UTF8.GetByteCount(file.Content);
            totalBytes += bytes;
            if (bytes > MaxFileBytes)
            {
                errors.Add(new ValidationError(file.Name, string.Empty, $"File is larger than {MaxFileBytes} bytes."));
            }

            safeFiles.Add(file);
        }

        if (totalBytes > MaxPackBytes)
        {
            errors.Add(new ValidationError("(pack)", string.Empty, $"Pack is larger than {MaxPackBytes} bytes."));
        }

        var manifest = safeFiles.FirstOrDefault(file => file.Name == ManifestName);
        if (manifest is null)
        {
            errors.Add(new ValidationError(ManifestName, string.Empty, "Missing pack manifest."));
            return new ValidationResult(errors);
        }

        var domainIds = new HashSet<string>(StringComparer.Ordinal);
        ValidateManifest(manifest, domainIds, errors);

        var questionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in safeFiles)
        {
            if (file.Name == ManifestName || file.Name == VerificationName)
            {
                continue;
            }

            if (file.Name == FlashcardsName)
            {
                ValidateFlashcards(file, errors);
            }
            else if (file.Name.StartsWith(TopicPrefix, StringComparison.Ordinal) && file.Name.EndsWith(".json", StringComparison.Ordinal))
            {
                ValidateTopic(file, domainIds, questionIds, errors);
            }
            else
            {
                errors.Add(new ValidationError(file.Name, string.Empty, "Unexpected file in pack."));
            }
        }

        return new ValidationResult(errors);
    }

    private static void ValidateManifest(PackFile file, HashSet<string> domainIds, List<ValidationError> errors)
    {
        var c = new PackChecker(file.Name, errors);
        if (c.ParseRoot(file.Content) is not { } root || !c.ObjectAt(root, string.Empty, ManifestFields))
        {
            return;
        }

        c.Int(root, string.Empty, "schemaVersion", 1, 1);
        c.Pattern(root, string.Empty, "packId", SlugPattern, "a lowercase slug");
        c.Pattern(root, string.Empty, "version", VersionPattern, "in the form YYYY.MM.N");

        if (c.Object(root, string.Empty, "exam", ["code", "name"]) is { } exam)
        {
            c.Text(exam, "exam", "code", 20);
            c.Text(exam, "exam", "name", 120);
        }

        var weightTotal = 0;
        if (c.Array(root, string.Empty, "domains", 1, 20) is { } domains)
        {
            for (var i = 0; i < domains.Length; i++)
            {
                var path = $"domains[{i}]";
                if (!c.ObjectAt(domains[i], path, ["id", "name", "weightPercent", "sortOrder"]))
                {
                    continue;
                }

                if (c.Pattern(domains[i], path, "id", SlugPattern, "a lowercase slug") is { } id && !domainIds.Add(id))
                {
                    c.Error(PackChecker.Join(path, "id"), "Duplicate domain id.");
                }

                c.Text(domains[i], path, "name", 120);
                if (c.Int(domains[i], path, "weightPercent", 0, 100) is { } weight)
                {
                    weightTotal += weight;
                }

                c.Int(domains[i], path, "sortOrder", 1, 100);
            }

            if (weightTotal != 100)
            {
                c.Error("domains", $"Domain weights must add up to 100 (found {weightTotal}).");
            }
        }

        if (c.Array(root, string.Empty, "sources", 1, 50) is { } sources)
        {
            for (var i = 0; i < sources.Length; i++)
            {
                var path = $"sources[{i}]";
                if (!c.ObjectAt(sources[i], path, ["url", "accessedOn"]))
                {
                    continue;
                }

                c.Url(sources[i], path, "url");
                c.Pattern(sources[i], path, "accessedOn", DatePattern, "a YYYY-MM-DD date");
            }
        }
    }

    private static void ValidateTopic(PackFile file, HashSet<string> domainIds, HashSet<string> questionIds, List<ValidationError> errors)
    {
        var c = new PackChecker(file.Name, errors);
        if (c.ParseRoot(file.Content) is not { } root || !c.ObjectAt(root, string.Empty, TopicFields))
        {
            return;
        }

        c.Int(root, string.Empty, "schemaVersion", 1, 1);
        c.Pattern(root, string.Empty, "topicId", SlugPattern, "a lowercase slug");
        if (c.Pattern(root, string.Empty, "domainId", SlugPattern, "a lowercase slug") is { } domainId && !domainIds.Contains(domainId))
        {
            c.Error("domainId", "Unknown domain. Add it to pack.json first.");
        }

        c.Text(root, string.Empty, "name", 120);

        if (c.Object(root, string.Empty, "lesson", ["summary", "keyPoints", "docLinks"]) is { } lesson)
        {
            c.Text(lesson, "lesson", "summary", 1000);
            if (c.Array(lesson, "lesson", "keyPoints", 1, 10) is { } points)
            {
                for (var i = 0; i < points.Length; i++)
                {
                    c.TextAt(points[i], $"lesson.keyPoints[{i}]", 300);
                }
            }

            UrlArray(c, lesson, "lesson", "docLinks", 1, 10);
        }

        var scenarioIds = new HashSet<string>(StringComparer.Ordinal);
        if (c.Array(root, string.Empty, "scenarios", 0, 20) is { } scenarios)
        {
            for (var i = 0; i < scenarios.Length; i++)
            {
                var path = $"scenarios[{i}]";
                if (!c.ObjectAt(scenarios[i], path, ["id", "text"]))
                {
                    continue;
                }

                if (c.Pattern(scenarios[i], path, "id", SlugPattern, "a lowercase slug") is { } scenarioId && !scenarioIds.Add(scenarioId))
                {
                    c.Error(PackChecker.Join(path, "id"), "Duplicate scenario id.");
                }

                c.Text(scenarios[i], path, "text", 2000);
            }
        }

        if (c.Array(root, string.Empty, "questions", 1, 200) is { } questions)
        {
            for (var i = 0; i < questions.Length; i++)
            {
                ValidateQuestion(c, questions[i], $"questions[{i}]", questionIds, scenarioIds);
            }
        }
    }

    private static void ValidateQuestion(PackChecker c, JsonElement question, string path, HashSet<string> questionIds, HashSet<string> scenarioIds)
    {
        if (question.ValueKind != JsonValueKind.Object)
        {
            c.Error(path, "Expected an object.");
            return;
        }

        if (!question.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || !TypeFields.ContainsKey(typeElement.GetString() ?? string.Empty))
        {
            c.Error(PackChecker.Join(path, "type"), $"Unknown question type. Use one of: {string.Join(", ", TypeFields.Keys)}.");
            return;
        }

        var type = typeElement.GetString()!;
        var allowed = QuestionCommonFields.Concat(TypeFields[type]).ToArray();
        if (!c.ObjectAt(question, path, allowed))
        {
            return;
        }

        if (c.Pattern(question, path, "id", SlugPattern, "a lowercase slug") is { } id && !questionIds.Add(id))
        {
            c.Error(PackChecker.Join(path, "id"), "Duplicate question id across the pack.");
        }

        c.Int(question, path, "difficulty", 1, 5);
        if (c.Array(question, path, "tags", 0, 10) is { } tags)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                c.TextAt(tags[i], $"{path}.tags[{i}]", 40);
            }
        }

        c.Text(question, path, "explanation", 2000);
        UrlArray(c, question, path, "sources", 1, 10);

        if (type == "truefalse")
        {
            c.Text(question, path, "statement", 500);
            c.Bool(question, path, "answer");
            return;
        }

        c.Text(question, path, "prompt", 1000);

        switch (type)
        {
            case "mcq":
                ValidateOptions(c, question, path, exactlyOne: true);
                break;
            case "multi":
                ValidateOptions(c, question, path, exactlyOne: false);
                break;
            case "scenario":
                ValidateOptions(c, question, path, exactlyOne: false);
                if (c.Pattern(question, path, "scenarioId", SlugPattern, "a lowercase slug") is { } scenarioId && !scenarioIds.Contains(scenarioId))
                {
                    c.Error(PackChecker.Join(path, "scenarioId"), "Unknown scenario in this topic.");
                }

                break;
            case "matching":
                ValidatePairs(c, question, path);
                break;
            case "ordering":
                ValidateOrdering(c, question, path);
                break;
            case "typed":
                ValidateRubric(c, question, path);
                break;
        }
    }

    /// Single-answer questions have exactly one correct option. Multi-response questions have
    /// at least one correct and at least one wrong option. Scenario questions follow the multi-response rule.
    private static void ValidateOptions(PackChecker c, JsonElement question, string path, bool exactlyOne)
    {
        if (c.Array(question, path, "options", 2, 8) is not { } options)
        {
            return;
        }

        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        var correct = 0;
        for (var i = 0; i < options.Length; i++)
        {
            var optionPath = $"{path}.options[{i}]";
            if (!c.ObjectAt(options[i], optionPath, ["id", "text", "correct"]))
            {
                continue;
            }

            if (c.Pattern(options[i], optionPath, "id", OptionIdPattern, "a short lowercase id") is { } optionId && !optionIds.Add(optionId))
            {
                c.Error(PackChecker.Join(optionPath, "id"), "Duplicate option id.");
            }

            c.Text(options[i], optionPath, "text", 500);
            if (c.Bool(options[i], optionPath, "correct") is true)
            {
                correct++;
            }
        }

        var valid = exactlyOne ? correct == 1 : correct >= 1 && correct < options.Length;
        if (!valid)
        {
            c.Error($"{path}.options", exactlyOne
                ? "Single-answer questions need exactly one correct option."
                : "Multiple-response questions need at least one correct option and at least one wrong option.");
        }
    }

    private static void ValidatePairs(PackChecker c, JsonElement question, string path)
    {
        if (c.Array(question, path, "pairs", 2, 10) is not { } pairs)
        {
            return;
        }

        for (var i = 0; i < pairs.Length; i++)
        {
            var pairPath = $"{path}.pairs[{i}]";
            if (!c.ObjectAt(pairs[i], pairPath, ["left", "right"]))
            {
                continue;
            }

            c.Text(pairs[i], pairPath, "left", 200);
            c.Text(pairs[i], pairPath, "right", 200);
        }
    }

    /// The correct order must use every step index exactly once, so it is a permutation.
    private static void ValidateOrdering(PackChecker c, JsonElement question, string path)
    {
        if (c.Array(question, path, "steps", 2, 10) is not { } steps)
        {
            return;
        }

        for (var i = 0; i < steps.Length; i++)
        {
            c.TextAt(steps[i], $"{path}.steps[{i}]", 300);
        }

        if (c.Array(question, path, "correctOrder", steps.Length, steps.Length) is not { } order)
        {
            return;
        }

        var seen = new HashSet<int>();
        for (var i = 0; i < order.Length; i++)
        {
            var orderPath = $"{path}.correctOrder[{i}]";
            if (c.IntAt(order[i], orderPath, 0, steps.Length - 1) is { } index && !seen.Add(index))
            {
                c.Error(orderPath, "Each step index may appear only once.");
            }
        }
    }

    private static void ValidateRubric(PackChecker c, JsonElement question, string path)
    {
        if (c.Array(question, path, "rubric", 1, 10) is not { } groups)
        {
            return;
        }

        for (var i = 0; i < groups.Length; i++)
        {
            var groupPath = $"{path}.rubric[{i}]";
            if (!c.ObjectAt(groups[i], groupPath, ["any"]))
            {
                continue;
            }

            if (c.Array(groups[i], groupPath, "any", 1, 10) is { } terms)
            {
                for (var j = 0; j < terms.Length; j++)
                {
                    c.TextAt(terms[j], $"{groupPath}.any[{j}]", 100);
                }
            }
        }
    }

    private static void ValidateFlashcards(PackFile file, List<ValidationError> errors)
    {
        var c = new PackChecker(file.Name, errors);
        if (c.ParseRoot(file.Content) is not { } root || !c.ObjectAt(root, string.Empty, FlashcardFileFields))
        {
            return;
        }

        c.Int(root, string.Empty, "schemaVersion", 1, 1);
        if (c.Array(root, string.Empty, "cards", 1, 1000) is not { } cards)
        {
            return;
        }

        var cardIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < cards.Length; i++)
        {
            var path = $"cards[{i}]";
            if (!c.ObjectAt(cards[i], path, ["id", "term", "definition", "sources"]))
            {
                continue;
            }

            if (c.Pattern(cards[i], path, "id", SlugPattern, "a lowercase slug") is { } id && !cardIds.Add(id))
            {
                c.Error(PackChecker.Join(path, "id"), "Duplicate flashcard id.");
            }

            c.Text(cards[i], path, "term", 200);
            c.Text(cards[i], path, "definition", 1000);
            UrlArray(c, cards[i], path, "sources", 1, 10);
        }
    }

    private static void UrlArray(PackChecker c, JsonElement obj, string path, string name, int min, int max)
    {
        if (c.Array(obj, path, name, min, max) is not { } urls)
        {
            return;
        }

        for (var i = 0; i < urls.Length; i++)
        {
            c.UrlAt(urls[i], $"{PackChecker.Join(path, name)}[{i}]");
        }
    }
}

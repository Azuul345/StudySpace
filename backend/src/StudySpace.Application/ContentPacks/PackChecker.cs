using System.Text.Json;
using System.Text.RegularExpressions;

namespace StudySpace.Application.ContentPacks;

/// Field-level checks shared by the pack validator. Each failure is recorded with its path.
/// A check that fails returns null, so the caller can skip the rest of that branch.
internal sealed class PackChecker
{
    public const int DefaultMaxText = 5000;

    private static readonly Regex MarkupPattern = new(@"<[a-zA-Z/!]", RegexOptions.CultureInvariant);

    private readonly string _file;
    private readonly List<ValidationError> _errors;

    public PackChecker(string file, List<ValidationError> errors)
    {
        _file = file;
        _errors = errors;
    }

    public void Error(string path, string message) => _errors.Add(new ValidationError(_file, path, message));

    public static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";

    public JsonElement? ParseRoot(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            Error(string.Empty, "Invalid JSON.");
            return null;
        }
    }

    public bool ObjectAt(JsonElement element, string path, IReadOnlyCollection<string> allowed)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Error(path, "Expected an object.");
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                Error(Join(path, property.Name), "Unknown property.");
            }
        }

        return true;
    }

    public JsonElement? Field(JsonElement obj, string path, string name)
    {
        if (obj.TryGetProperty(name, out var value))
        {
            return value;
        }

        Error(Join(path, name), "Required property is missing.");
        return null;
    }

    public string? TextAt(JsonElement? element, string path, int maxLength = DefaultMaxText)
    {
        if (element is not { } el)
        {
            return null;
        }

        if (el.ValueKind != JsonValueKind.String)
        {
            Error(path, "Expected text.");
            return null;
        }

        var text = el.GetString() ?? string.Empty;
        if (text.Trim().Length == 0)
        {
            Error(path, "Must not be empty.");
            return null;
        }

        if (text.Length > maxLength)
        {
            Error(path, $"Longer than {maxLength} characters.");
            return null;
        }

        if (MarkupPattern.IsMatch(text))
        {
            Error(path, "Plain text only: markup is not allowed.");
            return null;
        }

        return text;
    }

    public string? PatternAt(JsonElement? element, string path, Regex pattern, string description)
    {
        if (element is not { } el)
        {
            return null;
        }

        if (el.ValueKind != JsonValueKind.String)
        {
            Error(path, "Expected text.");
            return null;
        }

        var value = el.GetString() ?? string.Empty;
        if (!pattern.IsMatch(value))
        {
            Error(path, $"Must be {description}.");
            return null;
        }

        return value;
    }

    public string? UrlAt(JsonElement? element, string path)
    {
        if (element is not { } el)
        {
            return null;
        }

        var value = el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        if (value is null
            || value.Length > 2000
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            Error(path, "Must be an https:// URL.");
            return null;
        }

        return value;
    }

    public int? IntAt(JsonElement? element, string path, int min, int max)
    {
        if (element is not { } el)
        {
            return null;
        }

        if (el.ValueKind != JsonValueKind.Number || !el.TryGetInt32(out var value))
        {
            Error(path, "Expected a whole number.");
            return null;
        }

        if (value < min || value > max)
        {
            Error(path, $"Must be between {min} and {max}.");
            return null;
        }

        return value;
    }

    public bool? BoolAt(JsonElement? element, string path)
    {
        if (element is not { } el)
        {
            return null;
        }

        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return el.GetBoolean();
        }

        Error(path, "Expected true or false.");
        return null;
    }

    public JsonElement[]? ArrayAt(JsonElement? element, string path, int min, int max)
    {
        if (element is not { } el)
        {
            return null;
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            Error(path, "Expected a list.");
            return null;
        }

        var items = el.EnumerateArray().ToArray();
        if (items.Length < min || items.Length > max)
        {
            Error(path, $"Must have between {min} and {max} items.");
            return null;
        }

        return items;
    }

    public JsonElement? Object(JsonElement obj, string path, string name, IReadOnlyCollection<string> allowed)
    {
        if (Field(obj, path, name) is not { } element)
        {
            return null;
        }

        return ObjectAt(element, Join(path, name), allowed) ? element : null;
    }

    public string? Text(JsonElement obj, string path, string name, int maxLength = DefaultMaxText) =>
        TextAt(Field(obj, path, name), Join(path, name), maxLength);

    public string? Pattern(JsonElement obj, string path, string name, Regex pattern, string description) =>
        PatternAt(Field(obj, path, name), Join(path, name), pattern, description);

    public string? Url(JsonElement obj, string path, string name) =>
        UrlAt(Field(obj, path, name), Join(path, name));

    public int? Int(JsonElement obj, string path, string name, int min, int max) =>
        IntAt(Field(obj, path, name), Join(path, name), min, max);

    public bool? Bool(JsonElement obj, string path, string name) =>
        BoolAt(Field(obj, path, name), Join(path, name));

    public JsonElement[]? Array(JsonElement obj, string path, string name, int min, int max) =>
        ArrayAt(Field(obj, path, name), Join(path, name), min, max);
}

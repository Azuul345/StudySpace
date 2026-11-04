using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

[Trait("Req", "REQ-IMP-001")]
public class OptionalSimpleModeTests
{
    private static ValidationResult ValidateTopic(string topic) =>
        PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("topics/s3-security.json", topic),
            new("flashcards.json", PackSamples.Flashcards),
        ]);

    private static string WithLessonField(string field, string value) =>
        PackSamples.Topic.Replace("\"summary\":", $"\"{field}\": \"{value}\", \"summary\":", StringComparison.Ordinal);

    private static string WithQuestionField(string field, string value) =>
        PackSamples.Topic.Replace("\"explanation\":", $"\"{field}\": \"{value}\", \"explanation\":", StringComparison.Ordinal);

    [Fact]
    public void Validate_LessonSimpleSummaryAndMnemonic_AreOptionalAndAccepted()
    {
        var topic = WithLessonField("simpleSummary", "Plain words about the topic.").Replace(
            "\"summary\":", "\"mnemonic\": \"Naruto hint\", \"summary\":", StringComparison.Ordinal);

        var result = ValidateTopic(topic);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Path + ": " + e.Message)));
    }

    [Fact]
    public void Validate_QuestionSimplePromptSimpleExplanationAndMnemonic_AreOptionalAndAccepted()
    {
        var topic = WithQuestionField("simplePrompt", "A plain question.")
            .Replace("\"explanation\":", "\"simpleExplanation\": \"A plain reason.\", \"mnemonic\": \"Hint\", \"explanation\":", StringComparison.Ordinal);

        var result = ValidateTopic(topic);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Path + ": " + e.Message)));
    }

    [Theory]
    [InlineData("simpleSummary")]
    [InlineData("mnemonic")]
    public void Validate_LessonSimpleFields_RejectMarkup(string field)
    {
        var result = ValidateTopic(WithLessonField(field, "<b>bold</b>"));

        Assert.Contains(result.Errors, e => e.File == "topics/s3-security.json" && e.Path.Contains("lesson", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("simplePrompt")]
    [InlineData("simpleExplanation")]
    [InlineData("mnemonic")]
    public void Validate_QuestionSimpleFields_RejectMarkup(string field)
    {
        var result = ValidateTopic(WithQuestionField(field, "<script>x</script>"));

        Assert.Contains(result.Errors, e => e.File == "topics/s3-security.json" && e.Path.Contains("questions", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_MapsSimpleModeAndMnemonicFields()
    {
        var topic = WithQuestionField("simplePrompt", "A plain question.")
            .Replace("\"explanation\":", "\"simpleExplanation\": \"A plain reason.\", \"mnemonic\": \"Hint\", \"explanation\":", StringComparison.Ordinal);
        var files = new List<PackFile>(PackSamples.ValidPack());
        files[files.FindIndex(f => f.Name == "topics/s3-security.json")] = new("topics/s3-security.json", topic);

        var question = PackParser.Parse(files).Topics.Single().Questions.First();

        Assert.Equal("A plain question.", question.SimplePrompt);
        Assert.Equal("A plain reason.", question.SimpleExplanation);
        Assert.Equal("Hint", question.Mnemonic);
    }

    [Fact]
    public void Parse_WithoutSimpleFields_LeavesThemNull()
    {
        var question = PackParser.Parse(PackSamples.ValidPack()).Topics.Single().Questions.First();

        Assert.Null(question.SimplePrompt);
        Assert.Null(question.Mnemonic);
    }
}

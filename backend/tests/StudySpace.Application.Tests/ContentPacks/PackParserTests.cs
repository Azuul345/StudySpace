using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

[Trait("Req", "REQ-IMP-001")]
public class PackParserTests
{
    private static ParsedPack Parse(string? verification = null)
    {
        var files = PackSamples.ValidPack();
        if (verification is not null)
        {
            files.Add(new("verification.json", verification));
        }

        return PackParser.Parse(files);
    }

    [Fact]
    public void Parse_ReadsManifest()
    {
        var pack = Parse();

        Assert.Equal("aws-saa-c03", pack.PackId);
        Assert.Equal("2026.10.1", pack.Version);
        Assert.Equal("SAA-C03", pack.ExamCode);
        Assert.Equal(2, pack.Domains.Count);
        Assert.Equal(30, pack.Domains[0].WeightPercent);
    }

    [Fact]
    public void Parse_ReadsTopicLessonScenariosAndQuestions()
    {
        var topic = Assert.Single(Parse().Topics);

        Assert.Equal("s3-security", topic.Id);
        Assert.Equal("secure", topic.DomainId);
        Assert.Single(topic.KeyPoints);
        Assert.Single(topic.Scenarios);
        Assert.Equal(7, topic.Questions.Count);
    }

    [Fact]
    public void Parse_OptionQuestionsKeepCorrectFlags()
    {
        var multi = Parse().Topics[0].Questions.Single(q => q.Id == "s3sec-002");

        Assert.Equal("multi", multi.Type);
        Assert.Equal(["a", "c"], multi.Options.Where(o => o.IsCorrect).Select(o => o.Id));
    }

    [Fact]
    public void Parse_ScenarioQuestionKeepsScenarioId()
    {
        var question = Parse().Topics[0].Questions.Single(q => q.Id == "s3sec-003");

        Assert.Equal("sc-1", question.ScenarioId);
    }

    [Fact]
    public void Parse_TrueFalseUsesStatementAsPromptAndStoresAnswer()
    {
        var question = Parse().Topics[0].Questions.Single(q => q.Id == "s3sec-005");

        Assert.Equal("S3 bucket names are globally unique.", question.Prompt);
        Assert.Contains("true", question.DataJson);
    }

    [Theory]
    [InlineData("s3sec-004", "right")]
    [InlineData("s3sec-006", "correctOrder")]
    [InlineData("s3sec-007", "any")]
    public void Parse_TypeSpecificDataIsStored(string questionId, string expectedFragment)
    {
        var question = Parse().Topics[0].Questions.Single(q => q.Id == questionId);

        Assert.Contains(expectedFragment, question.DataJson);
    }

    [Fact]
    public void Parse_ReadsFlashcards()
    {
        var card = Assert.Single(Parse().Flashcards);

        Assert.Equal("fc-001", card.Id);
        Assert.Equal("KMS", card.Term);
    }

    [Fact]
    public void Parse_WithoutVerificationFile_IsPending()
    {
        Assert.Equal(VerificationStatus.Pending, Parse().Status);
    }

    [Theory]
    [InlineData("""{ "status": "verified" }""", VerificationStatus.Verified)]
    [InlineData("""{ "status": "failed" }""", VerificationStatus.Failed)]
    [InlineData("""{ "status": "pending" }""", VerificationStatus.Pending)]
    [InlineData("""{ "status": "hacked" }""", VerificationStatus.Pending)]
    [InlineData("not json", VerificationStatus.Pending)]
    [InlineData("""["verified"]""", VerificationStatus.Pending)]
    public void Parse_VerificationStatusIsReadSafely(string verification, VerificationStatus expected)
    {
        Assert.Equal(expected, Parse(verification).Status);
    }

    [Fact]
    public void Parse_ReadsQuestionCaveats()
    {
        var pack = Parse("""
            { "status": "verified", "items": [
              { "kind": "question", "id": "s3sec-001", "result": "pass", "caveat": "  Limit not on the page.  " },
              { "kind": "question", "id": "s3sec-002", "result": "pass" },
              { "kind": "question", "id": "s3sec-003", "result": "pass", "caveat": "  " },
              { "kind": "lesson", "id": "s3-security", "result": "pass", "caveat": "Lesson note." }
            ] }
            """);

        var caveat = Assert.Single(pack.Caveats!);
        Assert.Equal("s3sec-001", caveat.Key);
        Assert.Equal("Limit not on the page.", caveat.Value);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "items": "nope" }""")]
    [InlineData("""{ "items": [1, "x", null, { "kind": 5, "id": 6, "caveat": 7 }] }""")]
    public void Parse_MalformedCaveatsAreIgnored(string verification)
    {
        Assert.Empty(Parse(verification).Caveats!);
    }
}

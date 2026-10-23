using System.Text.Json;
using StudySpace.Application.Practice;

namespace StudySpace.Application.Tests.Practice;

public class QuestionGraderTests
{
    private static readonly GradableOption[] Options =
    [
        new("a", false), new("b", true), new("c", true),
    ];

    private static GradeOutcome Grade(string type, string answerJson, string? data = null, GradableOption[]? options = null)
    {
        using var doc = JsonDocument.Parse(answerJson);
        return QuestionGrader.Grade(new GradableQuestion(type, options ?? Options, data), doc.RootElement.Clone());
    }

    [Trait("Req", "REQ-Q-001")]
    [Theory]
    [InlineData("""{"selected":["b"]}""", true)]
    [InlineData("""{"selected":["a"]}""", false)]
    public void Mcq_ScoresTheSelectedOption(string answer, bool expected)
    {
        var options = new GradableOption[] { new("a", false), new("b", true) };

        var result = Grade("mcq", answer, options: options);

        Assert.True(result.Valid);
        Assert.Equal(expected, result.IsCorrect);
    }

    [Theory]
    [InlineData("""{"selected":["a","b"]}""")]
    [InlineData("""{"selected":[]}""")]
    [InlineData("""{"selected":["zzz"]}""")]
    [InlineData("""{"selected":[1]}""")]
    [InlineData("""{}""")]
    [InlineData("""[]""")]
    public void Mcq_RejectsMalformedAnswers(string answer)
    {
        var options = new GradableOption[] { new("a", false), new("b", true) };

        Assert.False(Grade("mcq", answer, options: options).Valid);
    }

    [Trait("Req", "REQ-Q-002")]
    [Theory]
    [InlineData("""{"selected":["b","c"]}""", true)]
    [InlineData("""{"selected":["c","b"]}""", true)]
    [InlineData("""{"selected":["b"]}""", false)]
    [InlineData("""{"selected":["a","b","c"]}""", false)]
    public void Multi_IsAllOrNothing(string answer, bool expected)
    {
        var result = Grade("multi", answer);

        Assert.True(result.Valid);
        Assert.Equal(expected, result.IsCorrect);
    }

    [Fact]
    public void Multi_RejectsDuplicateSelections()
    {
        Assert.False(Grade("multi", """{"selected":["b","b"]}""").Valid);
    }

    [Fact]
    public void Scenario_GradesLikeMulti()
    {
        Assert.True(Grade("scenario", """{"selected":["b","c"]}""").IsCorrect);
    }

    [Trait("Req", "REQ-Q-005")]
    [Theory]
    [InlineData("""{"value":true}""", true)]
    [InlineData("""{"value":false}""", false)]
    public void TrueFalse_ScoresOnTheAnswer(string answer, bool expected)
    {
        var result = Grade("truefalse", answer, "{\"answer\":true}");

        Assert.Equal(expected, result.IsCorrect);
    }

    [Fact]
    public void TrueFalse_RejectsNonBoolean()
    {
        Assert.False(Grade("truefalse", """{"value":"yes"}""", "{\"answer\":true}").Valid);
    }

    [Trait("Req", "REQ-Q-006")]
    [Theory]
    [InlineData("""{"order":[2,0,1]}""", true)]
    [InlineData("""{"order":[0,1,2]}""", false)]
    public void Ordering_NeedsTheFullSequence(string answer, bool expected)
    {
        var data = """{"steps":["x","y","z"],"correctOrder":[2,0,1]}""";

        var result = Grade("ordering", answer, data);

        Assert.True(result.Valid);
        Assert.Equal(expected, result.IsCorrect);
    }

    [Theory]
    [InlineData("""{"order":[0,1]}""")]
    [InlineData("""{"order":[0,0,1]}""")]
    [InlineData("""{"order":[0,1,5]}""")]
    public void Ordering_RejectsNonPermutations(string answer)
    {
        var data = """{"steps":["x","y","z"],"correctOrder":[2,0,1]}""";

        Assert.False(Grade("ordering", answer, data).Valid);
    }

    [Trait("Req", "REQ-Q-004")]
    [Theory]
    [InlineData("""{"pairs":[{"left":"KMS","right":"Keys"},{"left":"WAF","right":"Filter"}]}""", true)]
    [InlineData("""{"pairs":[{"left":"WAF","right":"Filter"},{"left":"KMS","right":"Keys"}]}""", true)]
    [InlineData("""{"pairs":[{"left":"KMS","right":"Filter"},{"left":"WAF","right":"Keys"}]}""", false)]
    public void Matching_AllPairsMustBeRight(string answer, bool expected)
    {
        var data = """[{"left":"KMS","right":"Keys"},{"left":"WAF","right":"Filter"}]""";

        var result = Grade("matching", answer, data);

        Assert.True(result.Valid);
        Assert.Equal(expected, result.IsCorrect);
    }

    [Fact]
    public void Matching_RejectsWrongPairCount()
    {
        var data = """[{"left":"KMS","right":"Keys"},{"left":"WAF","right":"Filter"}]""";

        Assert.False(Grade("matching", """{"pairs":[{"left":"KMS","right":"Keys"}]}""", data).Valid);
    }

    [Trait("Req", "REQ-Q-007")]
    [Theory]
    [InlineData("""{"text":"Use SSE-KMS encryption"}""", true)]
    [InlineData("""{"text":"nothing relevant"}""", false)]
    public void Typed_UsesTheKeywordRubricAndAsksForConfirmation(string answer, bool expected)
    {
        var data = """[{"any":["kms","sse"]},{"any":["encryption","encrypt"]}]""";

        var result = Grade("typed", answer, data);

        Assert.True(result.Valid);
        Assert.Equal(expected, result.IsCorrect);
        Assert.True(result.NeedsConfirmation);
    }

    [Fact]
    public void Typed_RejectsEmptyAndOversizedText()
    {
        var data = """[{"any":["kms"]}]""";

        Assert.False(Grade("typed", """{"text":"   "}""", data).Valid);
        Assert.False(Grade("typed", "{\"text\":\"" + new string('a', QuestionGrader.MaxTypedLength + 1) + "\"}", data).Valid);
    }

    [Fact]
    public void NonTypedQuestions_DoNotNeedConfirmation()
    {
        Assert.False(Grade("multi", """{"selected":["b","c"]}""").NeedsConfirmation);
    }

    [Fact]
    public void UnknownType_IsRejected()
    {
        Assert.False(Grade("essay", "{}").Valid);
    }
}

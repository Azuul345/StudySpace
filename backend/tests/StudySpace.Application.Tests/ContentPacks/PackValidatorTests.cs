using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

[Trait("Req", "REQ-IMP-001")]
public class PackValidatorTests
{
    private static ValidationResult ValidateWithManifest(string manifest) =>
        PackValidator.Validate(
        [
            new("pack.json", manifest),
            new("topics/s3-security.json", PackSamples.Topic),
            new("flashcards.json", PackSamples.Flashcards),
        ]);

    private static ValidationResult ValidateWithTopic(string topic) =>
        PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("topics/s3-security.json", topic),
            new("flashcards.json", PackSamples.Flashcards),
        ]);

    private static void AssertRejected(ValidationResult result, string file, string pathFragment)
    {
        Assert.Contains(result.Errors, e => e.File == file && e.Path.Contains(pathFragment, StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ValidPack_HasNoErrors()
    {
        var result = PackValidator.Validate(PackSamples.ValidPack());

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => $"{e.File} {e.Path}: {e.Message}")));
    }

    [Fact]
    public void Validate_MissingManifest_IsRejected()
    {
        var result = PackValidator.Validate([new("topics/s3-security.json", PackSamples.Topic)]);

        Assert.Contains(result.Errors, e => e.File == "pack.json" && e.Message == "Missing pack manifest.");
    }

    [Fact]
    public void Validate_InvalidJson_IsRejected()
    {
        var result = ValidateWithTopic("{");

        Assert.Contains(result.Errors, e => e.File == "topics/s3-security.json" && e.Message == "Invalid JSON.");
    }

    [Fact]
    public void Validate_ManifestUnknownProperty_IsRejected()
    {
        var manifest = PackSamples.Replace(PackSamples.Manifest, "\"packId\": \"aws-saa-c03\",", "\"packId\": \"aws-saa-c03\", \"extra\": 1,");

        AssertRejected(ValidateWithManifest(manifest), "pack.json", "extra");
    }

    [Fact]
    public void Validate_DomainWeightsNotHundred_IsRejected()
    {
        var manifest = PackSamples.Replace(PackSamples.Manifest, "\"weightPercent\": 70", "\"weightPercent\": 60");

        var result = ValidateWithManifest(manifest);

        Assert.Contains(result.Errors, e => e.Message.Contains("add up to 100", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_InvalidVersionFormat_IsRejected()
    {
        var manifest = PackSamples.Replace(PackSamples.Manifest, "\"version\": \"2026.10.1\"", "\"version\": \"2026-10-1\"");

        AssertRejected(ValidateWithManifest(manifest), "pack.json", "version");
    }

    [Fact]
    public void Validate_TopicUnknownDomain_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"domainId\": \"secure\"", "\"domainId\": \"nope\"");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "domainId");
    }

    [Fact]
    public void Validate_QuestionUnknownProperty_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"difficulty\": 1, \"tags\": [ \"S3\" ],", "\"difficulty\": 1, \"tags\": [ \"S3\" ], \"hint\": \"x\",");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[4].hint");
    }

    [Fact]
    public void Validate_SingleAnswerWithTwoCorrect_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "{ \"id\": \"a\", \"text\": \"Bucket ACL\", \"correct\": false }", "{ \"id\": \"a\", \"text\": \"Bucket ACL\", \"correct\": true }");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[0].options");
    }

    [Fact]
    public void Validate_MultiResponseWithEveryOptionCorrect_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "{ \"id\": \"b\", \"text\": \"SSL certificate\", \"correct\": false }", "{ \"id\": \"b\", \"text\": \"SSL certificate\", \"correct\": true }");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[1].options");
    }

    [Fact]
    public void Validate_MatchingPairMissingRight_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"left\": \"WAF\", \"right\": \"Filter web requests\"", "\"left\": \"WAF\"");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[3].pairs[1]");
    }

    [Fact]
    public void Validate_TrueFalseAnswerNotBoolean_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"answer\": true", "\"answer\": \"yes\"");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[4].answer");
    }

    [Fact]
    public void Validate_UnknownQuestionType_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"type\": \"truefalse\"", "\"type\": \"essay\"");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[4].type");
    }

    [Fact]
    public void Validate_ScenarioReferencesUnknownScenario_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"scenarioId\": \"sc-1\"", "\"scenarioId\": \"sc-9\"");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[2].scenarioId");
    }

    [Fact]
    public void Validate_NonHttpsSource_IsRejected()
    {
        var topic = PackSamples.Replace(
            PackSamples.Topic,
            "https://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html",
            "http://docs.aws.amazon.com/AmazonS3/latest/userguide/access-control-block-public-access.html");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "sources");
    }

    [Fact]
    public void Validate_MarkupInExplanation_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"B is correct. A bucket ACL works per bucket only.\"", "\"<script>B is correct.</script>\"");

        var result = ValidateWithTopic(topic);

        AssertRejected(result, "topics/s3-security.json", "questions[0].explanation");
        Assert.Contains(result.Errors, e => e.Message.Contains("Plain text only", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RepeatedOrderingIndex_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"correctOrder\": [ 0, 1, 2 ]", "\"correctOrder\": [ 0, 1, 1 ]");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[5].correctOrder");
    }

    [Fact]
    public void Validate_TypedWithEmptyRubric_IsRejected()
    {
        var topic = PackSamples.Replace(PackSamples.Topic, "\"rubric\": [ { \"any\": [ \"encrypt\", \"encryption\", \"sse\" ] } ]", "\"rubric\": []");

        AssertRejected(ValidateWithTopic(topic), "topics/s3-security.json", "questions[6].rubric");
    }

    [Fact]
    public void Validate_QuestionIdDuplicatedAcrossFiles_IsRejected()
    {
        var result = PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("topics/s3-security.json", PackSamples.Topic),
            new("topics/copy.json", PackSamples.Topic),
            new("flashcards.json", PackSamples.Flashcards),
        ]);

        AssertRejected(result, "topics/copy.json", "questions[0].id");
    }

    [Fact]
    public void Validate_FileLargerThanLimit_IsRejected()
    {
        var oversized = PackSamples.Flashcards + new string(' ', PackValidator.MaxFileBytes);

        var result = PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("flashcards.json", oversized),
        ]);

        Assert.Contains(result.Errors, e => e.File == "flashcards.json" && e.Message.Contains("larger than", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnsafeFilePath_IsRejected()
    {
        var result = PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("../evil.json", PackSamples.Topic),
        ]);

        Assert.Contains(result.Errors, e => e.File == "../evil.json" && e.Message == "Unsafe file path.");
    }

    [Fact]
    public void Validate_UnexpectedFile_IsRejected()
    {
        var result = PackValidator.Validate(
        [
            new("pack.json", PackSamples.Manifest),
            new("notes.txt", "hello"),
        ]);

        Assert.Contains(result.Errors, e => e.File == "notes.txt" && e.Message == "Unexpected file in pack.");
    }
}

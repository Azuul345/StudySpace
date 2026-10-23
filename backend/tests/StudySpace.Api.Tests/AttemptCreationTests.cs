using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

public class AttemptCreationTests : ApiTestBase
{
    [Trait("Req", "REQ-PRAC-003")]
    [Fact]
    public async Task Drill_ByTopic_ReturnsOnlyThatTopicsVerifiedQuestions()
    {
        var attempt = await StartDrill("High availability");

        Assert.Equal("drill", attempt.GetProperty("form").GetString());
        Assert.Equal(2, attempt.GetProperty("questions").GetArrayLength());
        Assert.True(attempt.GetProperty("attemptId").GetInt32() > 0);
    }

    [Fact]
    public async Task Drill_ByDomain_ReturnsQuestionsFromItsTopics()
    {
        var exam = await Client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");
        var domainId = exam.GetProperty("domains")[1].GetProperty("id").GetInt32();

        var attempt = await StartAttempt(new { examCode = "API-1", form = "drill", domainId });

        Assert.Equal(2, attempt.GetProperty("questions").GetArrayLength());
    }

    [Fact]
    public async Task Questions_NeverContainTheAnswers()
    {
        var attempt = await StartDrill("S3 security");
        var raw = attempt.GetRawText();

        Assert.DoesNotContain("\"correct\"", raw);
        Assert.DoesNotContain("\"explanation\"", raw);
        Assert.DoesNotContain("correctOrder", raw);
        Assert.DoesNotContain("\"rubric\"", raw);
        Assert.DoesNotContain("\"answer\"", raw);
    }

    [Fact]
    public async Task Questions_ExposeWhatEachFormatNeedsToRender()
    {
        var attempt = await StartDrill("S3 security");

        var mcq = Question(attempt, "Pick b.");
        Assert.Equal("mcq", mcq.GetProperty("type").GetString());
        Assert.Equal(["a", "b"], mcq.GetProperty("options").EnumerateArray().Select(o => o.GetProperty("id").GetString()));

        var order = Question(attempt, "Order.");
        Assert.Equal(["x", "y", "z"], order.GetProperty("steps").EnumerateArray().Select(s => s.GetString()));

        var match = Question(attempt, "Match.");
        Assert.Equal(2, match.GetProperty("pairs").GetProperty("lefts").GetArrayLength());
        Assert.Equal(2, match.GetProperty("pairs").GetProperty("rights").GetArrayLength());

        var tf = Question(attempt, "Water is wet.");
        Assert.Equal("truefalse", tf.GetProperty("type").GetString());
    }

    [Trait("Req", "REQ-Q-003")]
    [Trait("Req", "REQ-PRAC-005")]
    [Fact]
    public async Task ScenarioForm_ReturnsTheLinkedQuestionsWithTheScenarioText()
    {
        var drill = await StartDrill("S3 security");
        var scenario = Question(drill, "Scenario one?").GetProperty("scenario");
        Assert.Equal("A company stores logs.", scenario.GetProperty("text").GetString());

        var attempt = await StartAttempt(new { examCode = "API-1", form = "scenario", scenarioId = scenario.GetProperty("id").GetInt32() });

        Assert.Equal(["Scenario one?", "Scenario two?"], attempt.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("prompt").GetString()).Order());
    }

    [Trait("Req", "REQ-PRAC-001")]
    [Fact]
    public async Task FullExam_HasA130MinuteLimit()
    {
        var attempt = await StartAttempt(new { examCode = "API-1", form = "full" });

        Assert.Equal(130 * 60, attempt.GetProperty("timeLimitSeconds").GetInt32());
        Assert.Equal(10, attempt.GetProperty("questions").GetArrayLength());
    }

    [Trait("Req", "REQ-PRAC-004")]
    [Fact]
    public async Task DailyDrill_ReturnsQuestionsWithoutATimeLimit()
    {
        var attempt = await StartAttempt(new { examCode = "API-1", form = "daily" });

        Assert.Equal(JsonValueKind.Null, attempt.GetProperty("timeLimitSeconds").ValueKind);
        Assert.InRange(attempt.GetProperty("questions").GetArrayLength(), 1, 15);
    }

    [Trait("Req", "REQ-PRAC-002")]
    [Fact]
    public async Task BankForm_IsEmptyUntilSomethingIsAnsweredWrong()
    {
        var empty = await Client.PostAsJsonAsync("/api/v1/attempts", new { examCode = "API-1", form = "bank" });
        Assert.Equal(HttpStatusCode.Conflict, empty.StatusCode);

        var drill = await StartDrill("High availability");
        await Answer(drill, Question(drill, "HA one?"), new { selected = new[] { "b" } });

        var bank = await StartAttempt(new { examCode = "API-1", form = "bank" });
        Assert.Equal(["HA one?"], bank.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("prompt").GetString()));
    }

    [Trait("Req", "REQ-IMP-003")]
    [Fact]
    public async Task UnverifiedPack_NeverAppearsInAnyForm()
    {
        await Factory.SeedAsync(ApiSamples.PendingPack());

        foreach (var form in new[] { "full", "daily", "bank" })
        {
            var response = await Client.PostAsJsonAsync("/api/v1/attempts", new { examCode = "OTHER-1", form });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("""{"examCode":"API-1","form":"nope"}""")]
    [InlineData("""{"examCode":"API-1","form":"drill"}""")]
    [InlineData("""{"examCode":"API-1","form":"scenario"}""")]
    [InlineData("""{"examCode":"API-1","form":"drill","topicId":1,"count":0}""")]
    [InlineData("""{"examCode":"API-1","form":"drill","topicId":1,"count":101}""")]
    [InlineData("""{"form":"full"}""")]
    [InlineData("""{}""")]
    public async Task BadRequests_Are400(string body)
    {
        var response = await Client.PostAsync("/api/v1/attempts", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownExam_Is404()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/attempts", new { examCode = "NOPE", form = "full" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJson_Is400()
    {
        var response = await Client.PostAsync("/api/v1/attempts", new StringContent("{not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

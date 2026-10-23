using System.Net;
using System.Net.Http.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

public class AnswerEndpointTests : ApiTestBase
{
    [Trait("Req", "REQ-Q-001")]
    [Fact]
    public async Task Answer_Correct_ReturnsResultExplanationAndCorrectAnswer()
    {
        var attempt = await StartDrill("S3 security");

        var response = await Answer(attempt, Question(attempt, "Pick b."), new { selected = new[] { "b" } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True(body.GetProperty("isCorrect").GetBoolean());
        Assert.False(body.GetProperty("needsConfirmation").GetBoolean());
        Assert.Equal("B is right.", body.GetProperty("explanation").GetString());
        Assert.Equal(["b"], body.GetProperty("correct").GetProperty("selected").EnumerateArray().Select(e => e.GetString()));
    }

    [Trait("Req", "REQ-Q-002")]
    [Fact]
    public async Task Answer_MultiResponse_IsAllOrNothing()
    {
        var attempt = await StartDrill("S3 security");
        var question = Question(attempt, "Pick b and c.");

        var partial = await Json(await Answer(attempt, question, new { selected = new[] { "b" } }));

        Assert.False(partial.GetProperty("isCorrect").GetBoolean());
    }

    [Fact]
    public async Task Answer_EveryFormat_IsGradedOnTheServer()
    {
        var attempt = await StartDrill("S3 security");

        var tf = await Json(await Answer(attempt, Question(attempt, "Water is wet."), new { value = true }));
        var order = await Json(await Answer(attempt, Question(attempt, "Order."), new { order = new[] { 2, 0, 1 } }));
        var match = await Json(await Answer(attempt, Question(attempt, "Match."),
            new { pairs = new[] { new { left = "KMS", right = "Keys" }, new { left = "WAF", right = "Filter" } } }));

        Assert.True(tf.GetProperty("isCorrect").GetBoolean());
        Assert.True(order.GetProperty("isCorrect").GetBoolean());
        Assert.True(match.GetProperty("isCorrect").GetBoolean());
    }

    [Fact]
    public async Task Answer_BadShape_Is400()
    {
        var attempt = await StartDrill("S3 security");

        var response = await Answer(attempt, Question(attempt, "Pick b."), new { selected = new[] { "zzz" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Answer_QuestionNotInAttempt_Is400()
    {
        var attempt = await StartDrill("High availability");
        var other = await StartDrill("S3 security");

        var response = await Answer(attempt, Question(other, "Pick b."), new { selected = new[] { "b" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Answer_UnknownAttempt_Is404()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/attempts/9999/answers", new { questionId = 1, answer = new { } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Answer_SameQuestionTwice_Is409()
    {
        var attempt = await StartDrill("S3 security");
        var question = Question(attempt, "Pick b.");
        await Answer(attempt, question, new { selected = new[] { "b" } });

        var again = await Answer(attempt, question, new { selected = new[] { "b" } });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Answer_AfterFinish_Is409()
    {
        var attempt = await StartDrill("S3 security");
        await Finish(attempt);

        var response = await Answer(attempt, Question(attempt, "Pick b."), new { selected = new[] { "b" } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Trait("Req", "REQ-PRAC-001")]
    [Fact]
    public async Task Answer_AfterTheTimeLimit_Is409()
    {
        var attempt = await StartAttempt(new { examCode = "API-1", form = "full" });
        var question = attempt.GetProperty("questions")[0];
        Factory.Clock.Advance(TimeSpan.FromMinutes(131));

        var response = await Answer(attempt, question, new { selected = new[] { "a" } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Finish_ReturnsAScoreAndAReviewOfEveryQuestion()
    {
        var attempt = await StartDrill("High availability");
        await Answer(attempt, Question(attempt, "HA one?"), new { selected = new[] { "a" } });

        var response = await Finish(attempt);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await Json(response);
        Assert.Equal(2, summary.GetProperty("total").GetInt32());
        Assert.Equal(1, summary.GetProperty("answered").GetInt32());
        Assert.Equal(1, summary.GetProperty("correct").GetInt32());
        Assert.Equal(50, summary.GetProperty("percent").GetInt32());
        var results = summary.GetProperty("results").EnumerateArray().ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => !r.GetProperty("answered").GetBoolean() && r.GetProperty("explanation").GetString() == "A.");
    }

    [Fact]
    public async Task Finish_Twice_ReturnsTheSameSummary()
    {
        var attempt = await StartDrill("High availability");
        await Answer(attempt, Question(attempt, "HA one?"), new { selected = new[] { "a" } });

        var first = await Json(await Finish(attempt));
        var second = await Json(await Finish(attempt));

        Assert.Equal(first.GetProperty("finishedAt").GetString(), second.GetProperty("finishedAt").GetString());
        Assert.Equal(first.GetProperty("correct").GetInt32(), second.GetProperty("correct").GetInt32());
    }

    [Fact]
    public async Task Finish_UnknownAttempt_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PostAsync("/api/v1/attempts/9999/finish", null)).StatusCode);
    }
}

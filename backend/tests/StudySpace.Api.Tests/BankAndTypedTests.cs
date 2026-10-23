using System.Net;
using System.Net.Http.Json;
using StudySpace.Api.Tests.Support;
using StudySpace.Domain.Bank;

namespace StudySpace.Api.Tests;

public class BankAndTypedTests : ApiTestBase
{
    private async Task WrongThenRight(int rightCount)
    {
        var first = await StartDrill("High availability");
        await Answer(first, Question(first, "HA one?"), new { selected = new[] { "b" } });
        for (var i = 0; i < rightCount; i++)
        {
            var attempt = await StartDrill("High availability");
            await Answer(attempt, Question(attempt, "HA one?"), new { selected = new[] { "a" } });
        }
    }

    [Trait("Req", "REQ-BANK-001")]
    [Fact]
    public async Task WrongAnswer_EntersTheBank_AndFiveCorrectInARowMasterIt()
    {
        await WrongThenRight(4);
        var item = await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Single()));
        Assert.Equal(BankStatus.Active, item.Status);
        Assert.Equal(4, item.ConsecutiveCorrect);

        var fifth = await StartDrill("High availability");
        await Answer(fifth, Question(fifth, "HA one?"), new { selected = new[] { "a" } });

        item = await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Single()));
        Assert.Equal(BankStatus.Mastered, item.Status);
    }

    [Trait("Req", "REQ-BANK-002")]
    [Fact]
    public async Task WrongAnswer_ReactivatesAMasteredItem()
    {
        await WrongThenRight(5);

        var again = await StartDrill("High availability");
        await Answer(again, Question(again, "HA one?"), new { selected = new[] { "b" } });

        var item = await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Single()));
        Assert.Equal(BankStatus.Active, item.Status);
        Assert.Equal(0, item.ConsecutiveCorrect);
    }

    [Fact]
    public async Task CorrectAnswer_ForAQuestionNotInTheBank_DoesNotCreateAnItem()
    {
        var attempt = await StartDrill("High availability");
        await Answer(attempt, Question(attempt, "HA one?"), new { selected = new[] { "a" } });

        Assert.Equal(0, await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Count())));
    }

    [Trait("Req", "REQ-Q-007")]
    [Fact]
    public async Task Typed_IsGradedByKeywordThenConfirmedByTheUser()
    {
        var attempt = await StartDrill("S3 security");
        var question = Question(attempt, "Name an encryption service.");

        var graded = await Json(await Answer(attempt, question, new { text = "I would use KMS" }));
        Assert.True(graded.GetProperty("isCorrect").GetBoolean());
        Assert.True(graded.GetProperty("needsConfirmation").GetBoolean());

        var overridden = await Client.PostAsJsonAsync($"/api/v1/typed/{question.GetProperty("id").GetInt32()}/override",
            new { attemptId = attempt.GetProperty("attemptId").GetInt32(), correct = false });

        Assert.Equal(HttpStatusCode.OK, overridden.StatusCode);
        var stored = await Factory.QueryAsync(c => Task.FromResult(c.AnswerRecords.Single()));
        Assert.False(stored.IsCorrect);
        Assert.False(stored.SelfOverride);
        Assert.Equal(BankStatus.Active, (await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Single()))).Status);
    }

    [Fact]
    public async Task Typed_DoesNotTouchTheBankUntilConfirmed()
    {
        var attempt = await StartDrill("S3 security");

        await Answer(attempt, Question(attempt, "Name an encryption service."), new { text = "no idea" });

        Assert.Equal(0, await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Count())));
    }

    [Fact]
    public async Task Typed_FinishConfirmsWhatTheKeywordsDecided()
    {
        var attempt = await StartDrill("S3 security");
        await Answer(attempt, Question(attempt, "Name an encryption service."), new { text = "no idea" });

        await Finish(attempt);

        Assert.Equal(1, await Factory.QueryAsync(c => Task.FromResult(c.BankItems.Count())));
    }

    [Fact]
    public async Task Override_UnansweredIs404_TwiceIs409_NonTypedIs400()
    {
        var attempt = await StartDrill("S3 security");
        var typed = Question(attempt, "Name an encryption service.");
        var attemptId = attempt.GetProperty("attemptId").GetInt32();
        var typedId = typed.GetProperty("id").GetInt32();

        var unanswered = await Client.PostAsJsonAsync($"/api/v1/typed/{typedId}/override", new { attemptId, correct = true });
        Assert.Equal(HttpStatusCode.NotFound, unanswered.StatusCode);

        await Answer(attempt, typed, new { text = "kms" });
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/v1/typed/{typedId}/override", new { attemptId, correct = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync($"/api/v1/typed/{typedId}/override", new { attemptId, correct = false })).StatusCode);

        var mcq = Question(attempt, "Pick b.");
        await Answer(attempt, mcq, new { selected = new[] { "b" } });
        var notTyped = await Client.PostAsJsonAsync($"/api/v1/typed/{mcq.GetProperty("id").GetInt32()}/override", new { attemptId, correct = true });
        Assert.Equal(HttpStatusCode.BadRequest, notTyped.StatusCode);
    }
}

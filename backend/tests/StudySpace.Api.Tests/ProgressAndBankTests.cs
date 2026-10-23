using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

public class ProgressAndBankTests : ApiTestBase
{
    private async Task AnswerHaOneRightHaTwoWrong()
    {
        var attempt = await StartDrill("High availability");
        await Answer(attempt, Question(attempt, "HA one?"), new { selected = new[] { "a" } });
        await Answer(attempt, Question(attempt, "HA two?"), new { selected = new[] { "b" } });
    }

    private static JsonElement TopicOf(JsonElement progress, string name) =>
        progress.GetProperty("domains").EnumerateArray()
            .SelectMany(d => d.GetProperty("topics").EnumerateArray())
            .First(t => t.GetProperty("name").GetString() == name);

    [Trait("Req", "REQ-PROG-001")]
    [Trait("Req", "REQ-PROG-003")]
    [Fact]
    public async Task Progress_ShowsAccuracyAndMasteryPerTopicAndDomain()
    {
        await AnswerHaOneRightHaTwoWrong();

        var progress = await Client.GetFromJsonAsync<JsonElement>("/api/v1/progress?examCode=API-1");

        var ha = TopicOf(progress, "High availability");
        Assert.Equal(2, ha.GetProperty("answered").GetInt32());
        Assert.Equal(1, ha.GetProperty("correct").GetInt32());
        Assert.Equal(0.5, ha.GetProperty("accuracy").GetDouble(), 3);
        Assert.Equal("weak", ha.GetProperty("level").GetString());
        Assert.Equal(1, ha.GetProperty("activeBankCount").GetInt32());

        var s3 = TopicOf(progress, "S3 security");
        Assert.Equal(0, s3.GetProperty("answered").GetInt32());
        Assert.Equal("unseen", s3.GetProperty("level").GetString());

        var resilient = progress.GetProperty("domains").EnumerateArray().First(d => d.GetProperty("name").GetString() == "Resilient");
        Assert.Equal(2, resilient.GetProperty("answered").GetInt32());
        Assert.Equal(1, resilient.GetProperty("activeBankCount").GetInt32());
        Assert.Equal("weak", resilient.GetProperty("level").GetString());
    }

    [Trait("Req", "REQ-PROG-001")]
    [Fact]
    public async Task Progress_RecentAnswersCountMore()
    {
        var old = await StartDrill("High availability");
        await Answer(old, Question(old, "HA one?"), new { selected = new[] { "b" } });
        Factory.Clock.Advance(TimeSpan.FromDays(60));
        for (var i = 0; i < 3; i++)
        {
            var recent = await StartDrill("High availability");
            await Answer(recent, Question(recent, "HA two?"), new { selected = new[] { "a" } });
        }

        var progress = await Client.GetFromJsonAsync<JsonElement>("/api/v1/progress?examCode=API-1");

        Assert.Equal("strong", TopicOf(progress, "High availability").GetProperty("level").GetString());
    }

    [Fact]
    public async Task Progress_IgnoresTypedAnswersUntilConfirmed()
    {
        var attempt = await StartDrill("S3 security");
        await Answer(attempt, Question(attempt, "Name an encryption service."), new { text = "kms" });

        var progress = await Client.GetFromJsonAsync<JsonElement>("/api/v1/progress?examCode=API-1");

        Assert.Equal(0, TopicOf(progress, "S3 security").GetProperty("answered").GetInt32());
    }

    [Theory]
    [InlineData("/api/v1/progress", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/progress?examCode=NOPE", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/bank", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/bank?examCode=NOPE", HttpStatusCode.NotFound)]
    [InlineData("/api/v1/flashcards/due", HttpStatusCode.BadRequest)]
    [InlineData("/api/v1/flashcards/due?examCode=NOPE", HttpStatusCode.NotFound)]
    public async Task ExamCode_IsRequiredAndMustExist(string url, HttpStatusCode expected)
    {
        Assert.Equal(expected, (await Client.GetAsync(url)).StatusCode);
    }

    [Trait("Req", "REQ-PROG-003")]
    [Fact]
    public async Task Bank_ListsActiveItemsGroupedByCategory()
    {
        await AnswerHaOneRightHaTwoWrong();

        var bank = await Client.GetFromJsonAsync<JsonElement>("/api/v1/bank?examCode=API-1");

        Assert.Equal(1, bank.GetProperty("total").GetInt32());
        var category = Assert.Single(bank.GetProperty("categories").EnumerateArray());
        Assert.Equal("Resilient", category.GetProperty("domainName").GetString());
        var item = Assert.Single(category.GetProperty("items").EnumerateArray());
        Assert.Equal("HA two?", item.GetProperty("prompt").GetString());
        Assert.Equal("High availability", item.GetProperty("topicName").GetString());
        Assert.Equal(0, item.GetProperty("consecutiveCorrect").GetInt32());
    }

    [Trait("Req", "REQ-BANK-001")]
    [Fact]
    public async Task Bank_DropsMasteredItems()
    {
        var wrong = await StartDrill("High availability");
        await Answer(wrong, Question(wrong, "HA one?"), new { selected = new[] { "b" } });
        for (var i = 0; i < 5; i++)
        {
            var ok = await StartDrill("High availability");
            await Answer(ok, Question(ok, "HA one?"), new { selected = new[] { "a" } });
        }

        var bank = await Client.GetFromJsonAsync<JsonElement>("/api/v1/bank?examCode=API-1");

        Assert.Equal(0, bank.GetProperty("total").GetInt32());
        Assert.Equal(0, bank.GetProperty("categories").GetArrayLength());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

[Trait("Req", "REQ-FC-001")]
public class FlashcardEndpointTests : ApiTestBase
{
    private async Task<List<JsonElement>> Due(string query = "") =>
        (await Client.GetFromJsonAsync<JsonElement>($"/api/v1/flashcards/due?examCode=API-1{query}")).EnumerateArray().ToList();

    [Fact]
    public async Task Due_NewCardsAreOffered()
    {
        var due = await Due();

        Assert.Equal(["KMS", "WAF"], due.Select(c => c.GetProperty("term").GetString()).Order());
        Assert.All(due, c => Assert.True(c.GetProperty("isNew").GetBoolean()));
    }

    [Fact]
    public async Task Due_RespectsTheLimit()
    {
        Assert.Single(await Due("&limit=1"));
    }

    [Theory]
    [InlineData("&limit=0")]
    [InlineData("&limit=201")]
    public async Task Due_RejectsBadLimits(string query)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync($"/api/v1/flashcards/due?examCode=API-1{query}")).StatusCode);
    }

    [Fact]
    public async Task Review_SchedulesTheCardWithSm2()
    {
        var card = (await Due()).First(c => c.GetProperty("term").GetString() == "KMS");
        var id = card.GetProperty("id").GetInt32();

        var response = await Client.PostAsJsonAsync($"/api/v1/flashcards/{id}/review", new { quality = 4 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal(1, body.GetProperty("repetitions").GetInt32());
        Assert.Equal(1, body.GetProperty("intervalDays").GetInt32());
        Assert.Equal(Factory.Clock.GetUtcNow().AddDays(1), body.GetProperty("dueAt").GetDateTimeOffset());
        Assert.DoesNotContain(await Due(), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Review_CardComesBackWhenItIsDue()
    {
        var id = (await Due()).First().GetProperty("id").GetInt32();
        await Client.PostAsJsonAsync($"/api/v1/flashcards/{id}/review", new { quality = 5 });

        Factory.Clock.Advance(TimeSpan.FromDays(2));

        var back = (await Due()).Single(c => c.GetProperty("id").GetInt32() == id);
        Assert.False(back.GetProperty("isNew").GetBoolean());
    }

    [Fact]
    public async Task Review_ASecondPassGrowsTheInterval()
    {
        var id = (await Due()).First().GetProperty("id").GetInt32();
        await Client.PostAsJsonAsync($"/api/v1/flashcards/{id}/review", new { quality = 5 });
        Factory.Clock.Advance(TimeSpan.FromDays(2));

        var second = await Json(await Client.PostAsJsonAsync($"/api/v1/flashcards/{id}/review", new { quality = 5 }));

        Assert.Equal(2, second.GetProperty("repetitions").GetInt32());
        Assert.Equal(6, second.GetProperty("intervalDays").GetInt32());
    }

    [Theory]
    [InlineData("""{"quality":6}""")]
    [InlineData("""{"quality":-1}""")]
    [InlineData("""{}""")]
    public async Task Review_BadQuality_Is400(string body)
    {
        var id = (await Due()).First().GetProperty("id").GetInt32();

        var response = await Client.PostAsync($"/api/v1/flashcards/{id}/review", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Review_UnknownCard_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.PostAsJsonAsync("/api/v1/flashcards/9999/review", new { quality = 4 })).StatusCode);
    }

    [Trait("Req", "REQ-IMP-003")]
    [Fact]
    public async Task Due_HidesCardsFromUnverifiedPacks()
    {
        await Factory.SeedAsync(ApiSamples.VerifiedPack("2026.11.1", "pending"));

        Assert.Empty(await Due());
    }
}

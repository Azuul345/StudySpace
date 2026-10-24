using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

public class CheckingEndpointTests : ApiTestBase
{
    [Fact]
    public async Task NeedsChecking_ListsOnlyQuestionsWithACaveat_WithAnswerAndSources()
    {
        var body = await Client.GetFromJsonAsync<JsonElement>("/api/v1/needs-checking?examCode=API-1");

        Assert.Equal(1, body.GetProperty("total").GetInt32());
        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal("q-mcq", item.GetProperty("reference").GetString());
        Assert.Equal("Pick b.", item.GetProperty("prompt").GetString());
        Assert.Equal("Page does not state the limit.", item.GetProperty("caveat").GetString());
        Assert.Equal("b) B", item.GetProperty("answer").GetString());
        Assert.Equal("S3 security", item.GetProperty("topicName").GetString());
        Assert.Equal("Secure", item.GetProperty("domainName").GetString());
        Assert.Equal("https://example.com/s", item.GetProperty("sources")[0].GetString());
    }

    [Fact]
    public async Task NeedsChecking_IgnoresQuestionsFromUnverifiedPacks()
    {
        await Factory.SeedAsync(ApiSamples.PendingPack());

        var body = await Client.GetFromJsonAsync<JsonElement>("/api/v1/needs-checking?examCode=OTHER-1");

        Assert.Equal(0, body.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("?examCode=NOPE", HttpStatusCode.NotFound)]
    public async Task NeedsChecking_BadExam_IsAProblem(string query, HttpStatusCode expected)
    {
        Assert.Equal(expected, (await Client.GetAsync("/api/v1/needs-checking" + query)).StatusCode);
    }
}

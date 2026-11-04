using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;
using StudySpace.Application.ContentPacks;

namespace StudySpace.Api.Tests;

[Trait("Req", "REQ-IMP-001")]
public class SimpleModeApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        var files = ApiSamples.VerifiedPack();
        var index = files.FindIndex(f => f.Name == "topics/secure.json");
        var secure = files[index].Content
            .Replace("\"summary\": \"Summary one.\",", "\"summary\": \"Summary one.\", \"simpleSummary\": \"Plain summary.\", \"mnemonic\": \"Naruto hint.\",", StringComparison.Ordinal)
            .Replace("\"explanation\":", "\"simplePrompt\": \"Plain question.\", \"simpleExplanation\": \"Plain reason.\", \"explanation\":", StringComparison.Ordinal);
        files[index] = new PackFile("topics/secure.json", secure);
        await _factory.SeedAsync(files);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Topic_ReturnsSimpleSummaryAndMnemonicWhenThePackHasThem()
    {
        var exam = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");
        var topicId = exam.GetProperty("domains")[0].GetProperty("topics")[0].GetProperty("id").GetInt32();

        var topic = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/topics/{topicId}");

        Assert.Equal("Plain summary.", topic.GetProperty("lesson").GetProperty("simpleSummary").GetString());
        Assert.Equal("Naruto hint.", topic.GetProperty("lesson").GetProperty("mnemonic").GetString());
    }

    [Fact]
    public async Task Quiz_QuestionsCarryTheSimplePromptAndMnemonicFields()
    {
        var exam = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");
        var domainId = exam.GetProperty("domains")[0].GetProperty("id").GetInt32();
        var response = await _client.PostAsJsonAsync("/api/v1/attempts", new { examCode = "API-1", form = "drill", domainId });
        var attempt = await response.Content.ReadFromJsonAsync<JsonElement>();

        var questions = attempt.GetProperty("questions").EnumerateArray().ToList();

        Assert.NotEmpty(questions);
        Assert.All(questions, q => Assert.Equal("Plain question.", q.GetProperty("simplePrompt").GetString()));
    }
}

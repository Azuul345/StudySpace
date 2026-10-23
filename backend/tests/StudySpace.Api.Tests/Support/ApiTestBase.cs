using System.Net.Http.Json;
using System.Text.Json;

namespace StudySpace.Api.Tests.Support;

/// One API and one seeded database per test class instance. Helpers keep the tests short.
public abstract class ApiTestBase : IAsyncLifetime
{
    protected readonly ApiFactory Factory = new();
    protected HttpClient Client = null!;

    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await Factory.SeedAsync(ApiSamples.VerifiedPack());
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<int> TopicId(string name)
    {
        var exam = await Client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");
        return exam.GetProperty("domains").EnumerateArray()
            .SelectMany(d => d.GetProperty("topics").EnumerateArray())
            .First(t => t.GetProperty("name").GetString() == name).GetProperty("id").GetInt32();
    }

    protected async Task<JsonElement> StartAttempt(object body)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/attempts", body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    protected async Task<JsonElement> StartDrill(string topicName, int count = 50) =>
        await StartAttempt(new { examCode = "API-1", form = "drill", topicId = await TopicId(topicName), count });

    protected static JsonElement Question(JsonElement attempt, string prompt) =>
        attempt.GetProperty("questions").EnumerateArray().First(q => q.GetProperty("prompt").GetString() == prompt);

    protected Task<HttpResponseMessage> Answer(JsonElement attempt, JsonElement question, object answer) =>
        Client.PostAsJsonAsync(
            $"/api/v1/attempts/{attempt.GetProperty("attemptId").GetInt32()}/answers",
            new { questionId = question.GetProperty("id").GetInt32(), answer });

    protected Task<HttpResponseMessage> Finish(JsonElement attempt) =>
        Client.PostAsync($"/api/v1/attempts/{attempt.GetProperty("attemptId").GetInt32()}/finish", null);

    protected static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}

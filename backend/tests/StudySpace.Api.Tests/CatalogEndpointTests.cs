using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StudySpace.Api.Tests.Support;

namespace StudySpace.Api.Tests;

public class CatalogEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.SeedAsync(ApiSamples.VerifiedPack());
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Exams_ListsImportedExams()
    {
        var exams = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams");

        var exam = Assert.Single(exams.EnumerateArray());
        Assert.Equal("API-1", exam.GetProperty("code").GetString());
        Assert.Equal("API exam", exam.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Exam_ReturnsDomainsWithTopicsAndVerifiedQuestionCounts()
    {
        var exam = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");

        var domains = exam.GetProperty("domains").EnumerateArray().ToList();
        Assert.Equal(["Secure", "Resilient"], domains.Select(d => d.GetProperty("name").GetString()));
        Assert.Equal(30, domains[0].GetProperty("weightPercent").GetInt32());
        var topic = domains[0].GetProperty("topics")[0];
        Assert.Equal("S3 security", topic.GetProperty("name").GetString());
        Assert.Equal(8, topic.GetProperty("questionCount").GetInt32());
    }

    [Fact]
    public async Task Exam_Unknown_Is404Problem()
    {
        var response = await _client.GetAsync("/api/v1/exams/NOPE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Topic_ReturnsLessonAndCount()
    {
        var exam = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams/API-1");
        var topicId = exam.GetProperty("domains")[1].GetProperty("topics")[0].GetProperty("id").GetInt32();

        var topic = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/topics/{topicId}");

        Assert.Equal("High availability", topic.GetProperty("name").GetString());
        Assert.Equal("Summary two.", topic.GetProperty("lesson").GetProperty("summary").GetString());
        Assert.Equal("Multi-AZ", topic.GetProperty("lesson").GetProperty("keyPoints")[0].GetString());
        Assert.Equal("https://example.com/ha", topic.GetProperty("lesson").GetProperty("docLinks")[0].GetString());
        Assert.Equal(2, topic.GetProperty("questionCount").GetInt32());
        Assert.Equal("Resilient", topic.GetProperty("domain").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Topic_Unknown_Is404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/topics/9999")).StatusCode);
    }

    [Trait("Req", "REQ-IMP-003")]
    [Fact]
    public async Task Counts_IgnoreQuestionsFromUnverifiedPacks()
    {
        await _factory.SeedAsync(ApiSamples.PendingPack());

        var exam = await _client.GetFromJsonAsync<JsonElement>("/api/v1/exams/OTHER-1");

        var counts = exam.GetProperty("domains").EnumerateArray()
            .SelectMany(d => d.GetProperty("topics").EnumerateArray())
            .Select(t => t.GetProperty("questionCount").GetInt32());
        Assert.All(counts, c => Assert.Equal(0, c));
    }

    [Fact]
    public async Task Api_SendsSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/v1/exams");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Infrastructure.Tests;

[Trait("Req", "REQ-IMP-002")]
public class EfPackRepositoryTests
{
    private static ParsedPack Pack(string version = "2026.10.1", VerificationStatus status = VerificationStatus.Verified, string explanation = "Because.")
    {
        ParsedQuestion Question(string id) => new(
            id, "mcq", "Prompt " + id, explanation, 2, null, ["S3"], ["https://docs.aws.amazon.com/x"],
            [new ParsedOption("a", "Wrong", false), new ParsedOption("b", "Right", true)], null);

        var topic = new ParsedTopic(
            "s3-security", "secure", "S3 security", 1, "Summary", ["Point"], ["https://docs.aws.amazon.com/y"],
            [new ParsedScenario("sc-1", "Scenario text")], [Question("q-1"), Question("q-2")]);

        return new ParsedPack(
            "aws-saa-c03", version, "SAA-C03", "AWS SAA", status,
            [new ParsedDomain("secure", "Secure", 100, 1)], [topic],
            [new ParsedFlashcard("fc-1", "KMS", "Key service")]);
    }

    private static async Task Save(TestDb db, ParsedPack pack, string hash = "hash")
    {
        await using var context = db.NewContext();
        await new EfPackRepository(context).SaveAsync(pack, hash, CancellationToken.None);
    }

    [Fact]
    public async Task Save_StoresTheWholePack()
    {
        using var db = new TestDb();

        await Save(db, Pack());

        await using var c = db.NewContext();
        Assert.Equal(1, await c.Exams.CountAsync());
        Assert.Equal(1, await c.Domains.CountAsync());
        Assert.Equal(1, await c.Topics.CountAsync());
        Assert.Equal(1, await c.Lessons.CountAsync());
        Assert.Equal(1, await c.Scenarios.CountAsync());
        Assert.Equal(2, await c.Questions.CountAsync());
        Assert.Equal(4, await c.QuestionOptions.CountAsync());
        Assert.Equal(1, await c.Flashcards.CountAsync());
        var pack = await c.ContentPacks.SingleAsync();
        Assert.Equal("hash", pack.FileHash);
        Assert.Equal("2026.10.1", pack.Version);
    }

    [Theory]
    [InlineData(VerificationStatus.Verified)]
    [InlineData(VerificationStatus.Pending)]
    [InlineData(VerificationStatus.Failed)]
    public async Task Save_StoresVerificationStatusOnPackAndQuestions(VerificationStatus status)
    {
        using var db = new TestDb();

        await Save(db, Pack(status: status));

        await using var c = db.NewContext();
        Assert.Equal(status, (await c.ContentPacks.SingleAsync()).VerificationStatus);
        Assert.All(await c.Questions.ToListAsync(), q => Assert.Equal(status, q.VerificationStatus));
    }

    [Fact]
    public async Task Exists_IsTrueOnlyForTheSavedVersion()
    {
        using var db = new TestDb();
        await Save(db, Pack());

        await using var c = db.NewContext();
        var repo = new EfPackRepository(c);

        Assert.True(await repo.ExistsAsync("aws-saa-c03", "2026.10.1", CancellationToken.None));
        Assert.False(await repo.ExistsAsync("aws-saa-c03", "2026.10.2", CancellationToken.None));
        Assert.False(await repo.ExistsAsync("other-pack", "2026.10.1", CancellationToken.None));
    }

    [Fact]
    public async Task Save_NewVersion_UpdatesQuestionsInPlaceWithoutDuplicates()
    {
        using var db = new TestDb();
        await Save(db, Pack());

        await Save(db, Pack(version: "2026.11.1", explanation: "Better explanation."), "hash2");

        await using var c = db.NewContext();
        Assert.Equal(2, await c.ContentPacks.CountAsync());
        Assert.Equal(1, await c.Exams.CountAsync());
        Assert.Equal(2, await c.Questions.CountAsync());
        Assert.Equal(4, await c.QuestionOptions.CountAsync());
        Assert.Equal(1, await c.Flashcards.CountAsync());
        Assert.All(await c.Questions.ToListAsync(), q => Assert.Equal("Better explanation.", q.Explanation));
    }

    [Fact]
    public async Task Save_StoresOptionCorrectFlags()
    {
        using var db = new TestDb();

        await Save(db, Pack());

        await using var c = db.NewContext();
        var question = await c.Questions.Include(q => q.Options).FirstAsync(q => q.Slug == "q-1");
        Assert.Equal(["b"], question.Options.Where(o => o.IsCorrect).Select(o => o.Slug));
    }

    [Fact]
    public async Task Save_WhenAnythingFails_StoresNothing()
    {
        using var db = new TestDb();
        var good = Pack();
        var topic = good.Topics[0];
        var duplicateQuestion = topic.Questions[0] with { };
        var broken = good with { Topics = [topic with { Questions = [.. topic.Questions, duplicateQuestion] }] };

        await Assert.ThrowsAnyAsync<Exception>(() => Save(db, broken));

        await using var c = db.NewContext();
        Assert.Equal(0, await c.Exams.CountAsync());
        Assert.Equal(0, await c.Questions.CountAsync());
        Assert.Equal(0, await c.ContentPacks.CountAsync());
    }

    [Fact]
    public async Task Save_StoresLessonDiagrams_AndReplacesThemOnANewVersion()
    {
        using var db = new TestDb();
        var pack = Pack();
        await Save(db, pack with { Topics = [pack.Topics[0] with { Diagrams = ["vpc-public-private"] }] }, "h1");
        await Save(db, pack with { Version = "2026.10.2", Topics = [pack.Topics[0] with { Diagrams = ["sqs-decoupling"] }] }, "h2");

        await using var c = db.NewContext();
        Assert.Equal("[\"sqs-decoupling\"]", (await c.Lessons.SingleAsync()).DiagramsJson);
    }

    [Fact]
    public async Task Save_StoresCaveatsOnlyForTheQuestionsThatHaveOne_AndClearsThemOnANewVersion()
    {
        using var db = new TestDb();

        await Save(db, Pack() with { Caveats = new Dictionary<string, string> { ["q-1"] = "Not on the page." } }, "h1");
        await using (var c = db.NewContext())
        {
            Assert.Equal("Not on the page.", (await c.Questions.SingleAsync(q => q.Slug == "q-1")).VerificationCaveat);
            Assert.Null((await c.Questions.SingleAsync(q => q.Slug == "q-2")).VerificationCaveat);
        }

        await Save(db, Pack(version: "2026.10.2"), "h2");
        await using var after = db.NewContext();
        Assert.All(await after.Questions.ToListAsync(), q => Assert.Null(q.VerificationCaveat));
    }

    [Fact]
    public async Task FullImport_ThroughTheImporter_IsIdempotent()
    {
        using var db = new TestDb();

        for (var i = 0; i < 2; i++)
        {
            await using var c = db.NewContext();
            var result = await new PackImporter(new EfPackRepository(c)).ImportAsync(
                [new("pack.json", SamplePack.Manifest), new("topics/a.json", SamplePack.Topic)], CancellationToken.None);
            Assert.Equal(i == 0 ? ImportOutcome.Imported : ImportOutcome.AlreadyImported, result.Outcome);
        }

        await using var check = db.NewContext();
        Assert.Equal(1, await check.ContentPacks.CountAsync());
        Assert.Equal(1, await check.Questions.CountAsync());
    }
}

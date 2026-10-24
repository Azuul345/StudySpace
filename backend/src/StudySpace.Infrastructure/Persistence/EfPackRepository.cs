using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;

namespace StudySpace.Infrastructure.Persistence;

/// Saves a validated pack in one transaction. Rows are matched by the pack's own ids (slugs),
/// so a new pack version updates content in place and keeps database ids stable for later progress data.
public sealed class EfPackRepository(StudySpaceDbContext db) : IPackRepository
{
    public Task<bool> ExistsAsync(string packId, string version, CancellationToken ct) =>
        db.ContentPacks.AnyAsync(p => p.PackSlug == packId && p.Version == version, ct);

    public async Task SaveAsync(ParsedPack pack, string fileHash, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var exam = await db.Exams.FirstOrDefaultAsync(e => e.Code == pack.ExamCode, ct);
        if (exam is null)
        {
            exam = new ExamEntity { Code = pack.ExamCode };
            db.Exams.Add(exam);
        }

        exam.Name = pack.ExamName;
        exam.Version = pack.Version;

        var packRow = new ContentPackEntity
        {
            PackSlug = pack.PackId,
            Version = pack.Version,
            FileHash = fileHash,
            ImportedAt = DateTimeOffset.UtcNow,
            VerificationStatus = pack.Status,
        };
        await db.SaveChangesAsync(ct);
        packRow.ExamId = exam.Id;
        db.ContentPacks.Add(packRow);

        var domains = await UpsertDomains(exam.Id, pack, ct);
        var topics = await UpsertTopics(exam.Id, pack, domains, ct);
        await UpsertQuestions(exam.Id, packRow, pack, topics, ct);
        await UpsertFlashcards(exam.Id, packRow, pack, ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<Dictionary<string, DomainEntity>> UpsertDomains(int examId, ParsedPack pack, CancellationToken ct)
    {
        var existing = await db.Domains.Where(d => d.ExamId == examId).ToDictionaryAsync(d => d.Slug, ct);
        foreach (var parsed in pack.Domains)
        {
            if (!existing.TryGetValue(parsed.Id, out var row))
            {
                row = new DomainEntity { ExamId = examId, Slug = parsed.Id };
                db.Domains.Add(row);
                existing[parsed.Id] = row;
            }

            row.Name = parsed.Name;
            row.WeightPercent = parsed.WeightPercent;
            row.SortOrder = parsed.SortOrder;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<Dictionary<string, TopicEntity>> UpsertTopics(int examId, ParsedPack pack, Dictionary<string, DomainEntity> domains, CancellationToken ct)
    {
        var existing = await db.Topics.Include(t => t.Lesson).Include(t => t.Scenarios)
            .Where(t => t.ExamId == examId).ToDictionaryAsync(t => t.Slug, ct);

        foreach (var parsed in pack.Topics)
        {
            if (!existing.TryGetValue(parsed.Id, out var topic))
            {
                topic = new TopicEntity { ExamId = examId, Slug = parsed.Id };
                db.Topics.Add(topic);
                existing[parsed.Id] = topic;
            }

            topic.DomainId = domains[parsed.DomainId].Id;
            topic.Name = parsed.Name;
            topic.SortOrder = parsed.SortOrder;

            topic.Lesson ??= new LessonEntity();
            topic.Lesson.Summary = parsed.Summary;
            topic.Lesson.KeyPointsJson = JsonSerializer.Serialize(parsed.KeyPoints);
            topic.Lesson.DocLinksJson = JsonSerializer.Serialize(parsed.DocLinks);

            foreach (var scenario in parsed.Scenarios)
            {
                var row = topic.Scenarios.FirstOrDefault(s => s.Slug == scenario.Id);
                if (row is null)
                {
                    row = new ScenarioEntity { Slug = scenario.Id };
                    topic.Scenarios.Add(row);
                }

                row.Text = scenario.Text;
            }
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task UpsertQuestions(int examId, ContentPackEntity packRow, ParsedPack pack, Dictionary<string, TopicEntity> topics, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        var existing = await db.Questions.Include(q => q.Options)
            .Where(q => q.ExamId == examId).ToDictionaryAsync(q => q.Slug, ct);

        foreach (var topic in pack.Topics)
        {
            var topicRow = topics[topic.Id];
            foreach (var parsed in topic.Questions)
            {
                if (!existing.TryGetValue(parsed.Id, out var row))
                {
                    row = new QuestionEntity { ExamId = examId, Slug = parsed.Id };
                    db.Questions.Add(row);
                }

                row.TopicId = topicRow.Id;
                row.ScenarioId = parsed.ScenarioId is null ? null : topicRow.Scenarios.First(s => s.Slug == parsed.ScenarioId).Id;
                row.Type = parsed.Type;
                row.Prompt = parsed.Prompt;
                row.Explanation = parsed.Explanation;
                row.Difficulty = parsed.Difficulty;
                row.TagsJson = JsonSerializer.Serialize(parsed.Tags);
                row.SourcesJson = JsonSerializer.Serialize(parsed.Sources);
                row.DataJson = parsed.DataJson;
                row.PackId = packRow.Id;
                row.VerificationStatus = pack.Status;
                row.VerificationCaveat = pack.Caveats is { } caveats && caveats.TryGetValue(parsed.Id, out var caveat) ? caveat : null;

                row.Options.Clear();
                var order = 0;
                foreach (var option in parsed.Options)
                {
                    row.Options.Add(new QuestionOptionEntity { Slug = option.Id, Text = option.Text, IsCorrect = option.IsCorrect, SortOrder = order++ });
                }
            }
        }
    }

    private async Task UpsertFlashcards(int examId, ContentPackEntity packRow, ParsedPack pack, CancellationToken ct)
    {
        var existing = await db.Flashcards.Where(f => f.ExamId == examId).ToDictionaryAsync(f => f.Slug, ct);
        foreach (var parsed in pack.Flashcards)
        {
            if (!existing.TryGetValue(parsed.Id, out var row))
            {
                row = new FlashcardEntity { ExamId = examId, Slug = parsed.Id };
                db.Flashcards.Add(row);
            }

            row.Term = parsed.Term;
            row.Definition = parsed.Definition;
            row.PackId = packRow.Id;
        }
    }
}

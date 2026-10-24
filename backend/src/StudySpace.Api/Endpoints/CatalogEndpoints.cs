using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalog(this RouteGroupBuilder api)
    {
        api.MapGet("/exams", async (StudySpaceDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Exams.OrderBy(e => e.Code).Select(e => new { e.Code, e.Name, e.Version }).ToListAsync(ct)));

        api.MapGet("/exams/{code}", GetExam);
        api.MapGet("/topics/{id:int}", GetTopic);
    }

    private static async Task<IResult> GetExam(string code, StudySpaceDbContext db, CancellationToken ct)
    {
        var exam = await db.Exams.FirstOrDefaultAsync(e => e.Code == code, ct);
        if (exam is null)
        {
            return Results.Problem(statusCode: 404, title: "Exam not found.");
        }

        var counts = await db.Questions
            .Where(q => q.ExamId == exam.Id && q.VerificationStatus == VerificationStatus.Verified)
            .GroupBy(q => q.TopicId)
            .Select(g => new { TopicId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TopicId, x => x.Count, ct);

        var domains = await db.Domains.Where(d => d.ExamId == exam.Id).OrderBy(d => d.SortOrder).ToListAsync(ct);
        var topics = await db.Topics.Where(t => t.ExamId == exam.Id).OrderBy(t => t.SortOrder).ToListAsync(ct);

        return Results.Ok(new
        {
            exam.Code,
            exam.Name,
            exam.Version,
            Domains = domains.Select(d => new
            {
                d.Id,
                Slug = d.Slug,
                d.Name,
                d.WeightPercent,
                Topics = topics.Where(t => t.DomainId == d.Id).Select(t => new
                {
                    t.Id,
                    t.Slug,
                    t.Name,
                    QuestionCount = counts.GetValueOrDefault(t.Id),
                }),
            }),
        });
    }

    private static async Task<IResult> GetTopic(int id, StudySpaceDbContext db, CancellationToken ct)
    {
        var topic = await db.Topics.Include(t => t.Lesson).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (topic is null)
        {
            return Results.Problem(statusCode: 404, title: "Topic not found.");
        }

        var domain = await db.Domains.FirstAsync(d => d.Id == topic.DomainId, ct);
        var examCode = await db.Exams.Where(e => e.Id == topic.ExamId).Select(e => e.Code).FirstAsync(ct);
        var count = await db.Questions.CountAsync(q => q.TopicId == id && q.VerificationStatus == VerificationStatus.Verified, ct);

        return Results.Ok(new
        {
            topic.Id,
            topic.Slug,
            topic.Name,
            ExamCode = examCode,
            Domain = new { domain.Id, domain.Name },
            Lesson = topic.Lesson is null ? null : new
            {
                topic.Lesson.Summary,
                KeyPoints = System.Text.Json.JsonSerializer.Deserialize<List<string>>(topic.Lesson.KeyPointsJson),
                DocLinks = System.Text.Json.JsonSerializer.Deserialize<List<string>>(topic.Lesson.DocLinksJson),
                Diagrams = System.Text.Json.JsonSerializer.Deserialize<List<string>>(topic.Lesson.DiagramsJson),
            },
            QuestionCount = count,
        });
    }
}

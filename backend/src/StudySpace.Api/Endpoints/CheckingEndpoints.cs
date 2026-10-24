using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

/// Lists the questions a verifier could not fully confirm, so a person can look them up and report back.
/// This is an audit view: it deliberately shows the answer and the sources.
public static class CheckingEndpoints
{
    public static void MapChecking(this RouteGroupBuilder api) => api.MapGet("/needs-checking", GetNeedsChecking);

    private static async Task<IResult> GetNeedsChecking(string? examCode, StudySpaceDbContext db, CancellationToken ct)
    {
        var (exam, error) = await ProgressEndpoints.FindExam(examCode, db, ct);
        if (exam is null)
        {
            return error!;
        }

        var questions = await db.Questions.Include(q => q.Options)
            .Where(q => q.ExamId == exam.Id && q.VerificationStatus == VerificationStatus.Verified && q.VerificationCaveat != null)
            .ToListAsync(ct);
        var topics = await db.Topics.Where(t => t.ExamId == exam.Id).ToDictionaryAsync(t => t.Id, ct);
        var domains = await db.Domains.Where(d => d.ExamId == exam.Id).ToDictionaryAsync(d => d.Id, ct);

        var items = questions
            .OrderBy(q => domains[topics[q.TopicId].DomainId].SortOrder).ThenBy(q => topics[q.TopicId].SortOrder).ThenBy(q => q.Slug, StringComparer.Ordinal)
            .Select(q => new
            {
                QuestionId = q.Id,
                Reference = q.Slug,
                q.Type,
                q.Prompt,
                TopicId = q.TopicId,
                TopicName = topics[q.TopicId].Name,
                DomainName = domains[topics[q.TopicId].DomainId].Name,
                Caveat = q.VerificationCaveat,
                Answer = QuestionViews.AnswerText(q),
                q.Explanation,
                Sources = JsonSerializer.Deserialize<List<string>>(q.SourcesJson) ?? [],
            })
            .ToList();

        return Results.Ok(new { Total = items.Count, Items = items });
    }
}

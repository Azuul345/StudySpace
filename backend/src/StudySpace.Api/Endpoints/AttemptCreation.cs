using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;
using StudySpace.Application.Practice;
using StudySpace.Domain.Bank;
using StudySpace.Domain.Progress;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

public sealed record CreateAttemptRequest(string? ExamCode, string? Form, int? TopicId, int? DomainId, int? ScenarioId, int? Count);

/// Starts a practice attempt (REQ-PRAC-001..005). Only questions from verified packs are ever picked (REQ-IMP-003).
internal static class AttemptCreation
{
    private const int ExamQuestions = 65;
    private const int ExamSeconds = 130 * 60;
    private const int MaxCount = 100;
    private static readonly string[] Forms = ["full", "drill", "daily", "bank", "scenario"];

    private sealed record Row(int Id, int TopicId, int DomainId, int? ScenarioId);

    public static async Task<IResult> Create(CreateAttemptRequest? request, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var form = request?.Form;
        if (request is null || form is null || !Forms.Contains(form))
        {
            return Bad($"form must be one of: {string.Join(", ", Forms)}.");
        }

        if (string.IsNullOrWhiteSpace(request.ExamCode))
        {
            return Bad("examCode is required.");
        }

        var exam = await db.Exams.FirstOrDefaultAsync(e => e.Code == request.ExamCode, ct);
        if (exam is null)
        {
            return Results.Problem(statusCode: 404, title: "Exam not found.");
        }

        var count = request.Count;
        var (min, max, fallback) = form == "daily" ? (10, 15, 12) : (1, MaxCount, form == "drill" ? 10 : 20);
        if (count is not null && (count < min || count > max))
        {
            return Bad($"count must be between {min} and {max}.");
        }

        var now = clock.GetUtcNow();
        var rows = await LoadVerified(db, exam.Id, ct);
        List<int> ids;

        switch (form)
        {
            case "drill":
                if (request.TopicId is null && request.DomainId is null)
                {
                    return Bad("A drill needs a topicId or a domainId.");
                }

                var chosen = rows.Where(r => request.TopicId is { } t ? r.TopicId == t : r.DomainId == request.DomainId);
                ids = chosen.Select(r => r.Id).OrderBy(id => id).OrderBy(_ => Random.Shared.Next()).Take(count ?? fallback).ToList();
                break;
            case "scenario":
                if (request.ScenarioId is null)
                {
                    return Bad("A scenario attempt needs a scenarioId.");
                }

                ids = rows.Where(r => r.ScenarioId == request.ScenarioId).Select(r => r.Id).OrderBy(id => id).ToList();
                break;
            case "bank":
                var active = await db.BankItems.Where(b => b.Status == BankStatus.Active).Select(b => b.QuestionId).ToListAsync(ct);
                ids = rows.Where(r => active.Contains(r.Id)).Select(r => r.Id).OrderBy(id => id).Take(count ?? fallback).ToList();
                break;
            case "daily":
                ids = DailyDrillPlanner.Plan(await DailyPools(db, exam.Id, rows, now, ct), count ?? fallback);
                break;
            default:
                var weights = await db.Domains.Where(d => d.ExamId == exam.Id).ToDictionaryAsync(d => d.Id, d => d.WeightPercent, ct);
                ids = ExamPlanner.Pick(rows.Select(r => new PlanCandidate(r.Id, r.DomainId)).ToList(), weights, ExamQuestions, (int)(now.UtcTicks % int.MaxValue));
                break;
        }

        if (ids.Count == 0)
        {
            return Results.Problem(statusCode: 409, title: "No verified questions are available for this practice form.");
        }

        var attempt = new AttemptEntity
        {
            ExamId = exam.Id,
            Form = form,
            StartedAt = now,
            TimeLimitSeconds = form == "full" ? ExamSeconds : null,
            Questions = ids.Select((id, position) => new AttemptQuestionEntity { QuestionId = id, Position = position }).ToList(),
        };
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        var questions = await db.Questions.Include(q => q.Options).Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id, ct);
        var scenarios = await db.Scenarios.ToDictionaryAsync(s => s.Id, ct);

        return Results.Json(new
        {
            AttemptId = attempt.Id,
            attempt.Form,
            attempt.StartedAt,
            attempt.TimeLimitSeconds,
            Questions = ids.Select(id => QuestionViews.ForAnswering(questions[id], questions[id].ScenarioId is { } s ? scenarios[s] : null)),
        }, statusCode: 201);
    }

    private static IResult Bad(string detail) => Results.Problem(statusCode: 400, title: "Invalid request.", detail: detail);

    private static async Task<List<Row>> LoadVerified(StudySpaceDbContext db, int examId, CancellationToken ct)
    {
        var topicDomain = await db.Topics.Where(t => t.ExamId == examId).ToDictionaryAsync(t => t.Id, t => t.DomainId, ct);
        var questions = await db.Questions
            .Where(q => q.ExamId == examId && q.VerificationStatus == VerificationStatus.Verified)
            .Select(q => new { q.Id, q.TopicId, q.ScenarioId })
            .ToListAsync(ct);
        return questions.Select(q => new Row(q.Id, q.TopicId, topicDomain[q.TopicId], q.ScenarioId)).ToList();
    }

    /// One pool per topic. Weak and unseen topics weigh more, and within a topic unseen and least recently asked questions come first.
    private static async Task<List<TopicPool>> DailyPools(StudySpaceDbContext db, int examId, List<Row> rows, DateTimeOffset now, CancellationToken ct)
    {
        var answers = await db.AnswerRecords
            .Where(a => a.Confirmed)
            .Join(db.Questions.Where(q => q.ExamId == examId), a => a.QuestionId, q => q.Id, (a, q) => new { q.TopicId, a.QuestionId, a.AnsweredAt, a.IsCorrect })
            .ToListAsync(ct);

        return rows.GroupBy(r => r.TopicId).Select(g =>
        {
            var weight = MasteryCalculator.ReviewWeight(MasteryCalculator.Calculate(
                answers.Where(a => a.TopicId == g.Key).Select(a => new AnswerEvent(a.AnsweredAt, a.IsCorrect)), now));
            var lastAsked = answers.Where(a => a.TopicId == g.Key).GroupBy(a => a.QuestionId).ToDictionary(x => x.Key, x => x.Max(a => a.AnsweredAt));
            var ordered = g.Select(r => r.Id)
                .OrderBy(id => lastAsked.TryGetValue(id, out var at) ? at : DateTimeOffset.MinValue)
                .ThenBy(id => id)
                .ToList();
            return new TopicPool(g.Key, weight, ordered);
        }).ToList();
    }
}

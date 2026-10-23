using Microsoft.EntityFrameworkCore;
using StudySpace.Domain.Bank;
using StudySpace.Domain.Progress;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

/// Mastery per topic and domain (REQ-PROG-001, REQ-PROG-003) and the wrong-answer bank list (REQ-BANK-001).
public static class ProgressEndpoints
{
    private sealed record Answered(int TopicId, DateTimeOffset At, bool IsCorrect);
    private sealed record Stats(int Answered, int Correct, double Accuracy, double Score, string Level, int ActiveBankCount);
    private sealed record TopicProgress(int Id, string Name, int Answered, int Correct, double Accuracy, double Score, string Level, int ActiveBankCount);
    private sealed record DomainProgress(int Id, string Name, int WeightPercent, int Answered, int Correct, double Accuracy, double Score, string Level, int ActiveBankCount, List<TopicProgress> Topics);

    public static void MapProgress(this RouteGroupBuilder api)
    {
        api.MapGet("/progress", GetProgress);
        api.MapGet("/bank", GetBank);
    }

    internal static async Task<(ExamEntity? Exam, IResult? Error)> FindExam(string? examCode, StudySpaceDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(examCode))
        {
            return (null, Results.Problem(statusCode: 400, title: "Invalid request.", detail: "examCode is required."));
        }

        var exam = await db.Exams.FirstOrDefaultAsync(e => e.Code == examCode, ct);
        return exam is null ? (null, Results.Problem(statusCode: 404, title: "Exam not found.")) : (exam, null);
    }

    private static async Task<IResult> GetProgress(string? examCode, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var (exam, error) = await FindExam(examCode, db, ct);
        if (exam is null)
        {
            return error!;
        }

        var now = clock.GetUtcNow();
        var domains = await db.Domains.Where(d => d.ExamId == exam.Id).OrderBy(d => d.SortOrder).ToListAsync(ct);
        var topics = await db.Topics.Where(t => t.ExamId == exam.Id).OrderBy(t => t.SortOrder).ToListAsync(ct);
        var answers = (await db.AnswerRecords
                .Where(a => a.Confirmed)
                .Join(db.Questions.Where(q => q.ExamId == exam.Id), a => a.QuestionId, q => q.Id, (a, q) => new { q.TopicId, a.AnsweredAt, a.IsCorrect })
                .ToListAsync(ct))
            .Select(a => new Answered(a.TopicId, a.AnsweredAt, a.IsCorrect)).ToList();
        var bank = await db.BankItems.Where(b => b.Status == BankStatus.Active)
            .Join(db.Questions.Where(q => q.ExamId == exam.Id), b => b.QuestionId, q => q.Id, (b, q) => q.TopicId)
            .ToListAsync(ct);

        Stats Summarize(IReadOnlyCollection<int> topicIds)
        {
            var mine = answers.Where(a => topicIds.Contains(a.TopicId)).ToList();
            var mastery = MasteryCalculator.Calculate(mine.Select(a => new AnswerEvent(a.At, a.IsCorrect)), now);
            var correct = mine.Count(a => a.IsCorrect);
            return new Stats(
                mine.Count, correct, mine.Count == 0 ? 0.0 : (double)correct / mine.Count,
                mastery.Score, mastery.Level.ToString().ToLowerInvariant(), bank.Count(t => topicIds.Contains(t)));
        }

        var result = domains.Select(d =>
        {
            var domainTopics = topics.Where(t => t.DomainId == d.Id).ToList();
            var s = Summarize(domainTopics.Select(t => t.Id).ToList());
            return new DomainProgress(
                d.Id, d.Name, d.WeightPercent, s.Answered, s.Correct, s.Accuracy, s.Score, s.Level, s.ActiveBankCount,
                domainTopics.Select(t =>
                {
                    var ts = Summarize([t.Id]);
                    return new TopicProgress(t.Id, t.Name, ts.Answered, ts.Correct, ts.Accuracy, ts.Score, ts.Level, ts.ActiveBankCount);
                }).ToList());
        }).ToList();

        return Results.Ok(new { exam.Code, Domains = result });
    }

    private static async Task<IResult> GetBank(string? examCode, StudySpaceDbContext db, CancellationToken ct)
    {
        var (exam, error) = await FindExam(examCode, db, ct);
        if (exam is null)
        {
            return error!;
        }

        var rows = await db.BankItems.Where(b => b.Status == BankStatus.Active)
            .Join(db.Questions.Where(q => q.ExamId == exam.Id), b => b.QuestionId, q => q.Id, (b, q) => new { b.QuestionId, b.ConsecutiveCorrect, b.LastWrongAt, q.Prompt, q.TopicId })
            .ToListAsync(ct);
        var topics = await db.Topics.Where(t => t.ExamId == exam.Id).ToDictionaryAsync(t => t.Id, ct);
        var domains = await db.Domains.Where(d => d.ExamId == exam.Id).OrderBy(d => d.SortOrder).ToListAsync(ct);

        var categories = domains
            .Select(d => new
            {
                DomainId = d.Id,
                DomainName = d.Name,
                Items = rows.Where(r => topics[r.TopicId].DomainId == d.Id)
                    .OrderBy(r => r.LastWrongAt).ThenBy(r => r.QuestionId)
                    .Select(r => new { r.QuestionId, r.TopicId, TopicName = topics[r.TopicId].Name, r.Prompt, r.ConsecutiveCorrect, r.LastWrongAt })
                    .ToList(),
            })
            .Where(c => c.Items.Count > 0)
            .ToList();

        return Results.Ok(new { Total = rows.Count, Categories = categories });
    }
}

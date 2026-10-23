using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;
using StudySpace.Domain.Scheduling;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

public sealed record ReviewRequest(int? Quality);

/// Glossary flashcards scheduled with SM-2 (REQ-FC-001). Cards from packs that are not verified are never offered.
public static class FlashcardEndpoints
{
    private const double StartingEase = 2.5;
    private const int DefaultLimit = 20;
    private const int MaxLimit = 200;

    public static void MapFlashcards(this RouteGroupBuilder api)
    {
        api.MapGet("/flashcards/due", GetDue);
        api.MapPost("/flashcards/{id:int}/review", Review);
    }

    private static async Task<IResult> GetDue(string? examCode, int? limit, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var (exam, error) = await ProgressEndpoints.FindExam(examCode, db, ct);
        if (exam is null)
        {
            return error!;
        }

        var take = limit ?? DefaultLimit;
        if (take is < 1 or > MaxLimit)
        {
            return Results.Problem(statusCode: 400, title: "Invalid request.", detail: $"limit must be between 1 and {MaxLimit}.");
        }

        var cards = await db.Flashcards
            .Where(f => f.ExamId == exam.Id)
            .Join(db.ContentPacks.Where(p => p.VerificationStatus == VerificationStatus.Verified), f => f.PackId, p => p.Id, (f, p) => f)
            .ToListAsync(ct);
        var schedules = await db.FlashcardSchedules.ToDictionaryAsync(s => s.FlashcardId, ct);
        var now = clock.GetUtcNow();

        var due = cards
            .Select(c => (Card: c, Schedule: schedules.GetValueOrDefault(c.Id)))
            .Where(x => x.Schedule is null || x.Schedule.DueAt <= now)
            .OrderBy(x => x.Schedule is null ? 1 : 0).ThenBy(x => x.Schedule?.DueAt).ThenBy(x => x.Card.Id)
            .Take(take)
            .Select(x => new { x.Card.Id, x.Card.Term, x.Card.Definition, IsNew = x.Schedule is null })
            .ToList();

        return Results.Ok(due);
    }

    private static async Task<IResult> Review(int id, ReviewRequest? request, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (request?.Quality is not { } quality || quality is < 0 or > 5)
        {
            return Results.Problem(statusCode: 400, title: "Invalid request.", detail: "quality must be a whole number from 0 to 5.");
        }

        if (!await db.Flashcards.AnyAsync(f => f.Id == id, ct))
        {
            return Results.Problem(statusCode: 404, title: "Flashcard not found.");
        }

        var schedule = await db.FlashcardSchedules.FindAsync([id], ct);
        if (schedule is null)
        {
            schedule = new FlashcardScheduleEntity { FlashcardId = id, EaseFactor = StartingEase };
            db.FlashcardSchedules.Add(schedule);
        }

        var next = Sm2Scheduler.Review(new Sm2Card(schedule.Repetitions, schedule.IntervalDays, schedule.EaseFactor), quality);
        schedule.Repetitions = next.Repetitions;
        schedule.IntervalDays = next.IntervalDays;
        schedule.EaseFactor = next.EaseFactor;
        schedule.DueAt = clock.GetUtcNow().AddDays(next.IntervalDays);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { Id = id, schedule.Repetitions, schedule.IntervalDays, schedule.EaseFactor, schedule.DueAt });
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudySpace.Application.Practice;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

public sealed record AnswerRequest(int? QuestionId, JsonElement Answer);

public sealed record OverrideRequest(int? AttemptId, bool? Correct);

public static class AttemptEndpoints
{
    /// Network delay allowance after the time limit, so an answer sent just before time ran out still counts.
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(10);

    public static void MapAttempts(this RouteGroupBuilder api)
    {
        api.MapPost("/attempts", AttemptCreation.Create);
        api.MapPost("/attempts/{id:int}/answers", SubmitAnswer);
        api.MapPost("/attempts/{id:int}/finish", FinishAttempt);
        api.MapPost("/typed/{questionId:int}/override", OverrideTyped);
    }

    private static async Task<IResult> SubmitAnswer(int id, AnswerRequest? request, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var attempt = await db.Attempts.Include(a => a.Questions).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attempt is null)
        {
            return Results.Problem(statusCode: 404, title: "Attempt not found.");
        }

        var now = clock.GetUtcNow();
        if (attempt.FinishedAt is not null)
        {
            return Results.Problem(statusCode: 409, title: "This attempt is already finished.");
        }

        if (attempt.TimeLimitSeconds is { } limit && now > attempt.StartedAt.AddSeconds(limit) + Grace)
        {
            return Results.Problem(statusCode: 409, title: "The time limit for this attempt has passed.");
        }

        if (request?.QuestionId is not { } questionId || attempt.Questions.All(q => q.QuestionId != questionId))
        {
            return Results.Problem(statusCode: 400, title: "Invalid request.", detail: "questionId is not part of this attempt.");
        }

        if (await db.AnswerRecords.AnyAsync(a => a.AttemptId == id && a.QuestionId == questionId, ct))
        {
            return Results.Problem(statusCode: 409, title: "This question is already answered in this attempt.");
        }

        var question = await db.Questions.Include(q => q.Options).FirstAsync(q => q.Id == questionId, ct);
        var grade = QuestionGrader.Grade(
            new GradableQuestion(question.Type, question.Options.Select(o => new GradableOption(o.Slug, o.IsCorrect)).ToList(), question.DataJson),
            request.Answer);
        if (!grade.Valid)
        {
            return Results.Problem(statusCode: 400, title: "Invalid answer.", detail: grade.Error);
        }

        db.AnswerRecords.Add(new AnswerRecordEntity
        {
            AttemptId = id,
            QuestionId = questionId,
            AnsweredAt = now,
            GivenAnswerJson = request.Answer.GetRawText(),
            IsCorrect = grade.IsCorrect,
            Confirmed = !grade.NeedsConfirmation,
        });
        if (!grade.NeedsConfirmation)
        {
            await BankUpdater.ApplyAsync(db, questionId, grade.IsCorrect, now, ct);
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            QuestionId = questionId,
            grade.IsCorrect,
            grade.NeedsConfirmation,
            question.Explanation,
            Correct = QuestionViews.CorrectAnswer(question),
        });
    }

    /// The user confirms or overrides the keyword result of a typed answer (REQ-Q-007). The verdict is stored.
    private static async Task<IResult> OverrideTyped(int questionId, OverrideRequest? request, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        if (request?.AttemptId is not { } attemptId || request.Correct is not { } correct)
        {
            return Results.Problem(statusCode: 400, title: "Invalid request.", detail: "attemptId and correct are required.");
        }

        if (!await db.Attempts.AnyAsync(a => a.Id == attemptId, ct))
        {
            return Results.Problem(statusCode: 404, title: "Attempt not found.");
        }

        var question = await db.Questions.FirstOrDefaultAsync(q => q.Id == questionId, ct);
        if (question is null)
        {
            return Results.Problem(statusCode: 404, title: "Question not found.");
        }

        if (question.Type != "typed")
        {
            return Results.Problem(statusCode: 400, title: "Only typed answers can be overridden.");
        }

        var record = await db.AnswerRecords.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);
        if (record is null)
        {
            return Results.Problem(statusCode: 404, title: "No answer to confirm.");
        }

        if (record.Confirmed)
        {
            return Results.Problem(statusCode: 409, title: "This answer is already confirmed.");
        }

        record.IsCorrect = correct;
        record.SelfOverride = correct;
        record.Confirmed = true;
        await BankUpdater.ApplyAsync(db, questionId, correct, clock.GetUtcNow(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { QuestionId = questionId, IsCorrect = correct });
    }

    private static async Task<IResult> FinishAttempt(int id, StudySpaceDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var attempt = await db.Attempts.Include(a => a.Questions).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attempt is null)
        {
            return Results.Problem(statusCode: 404, title: "Attempt not found.");
        }

        var now = clock.GetUtcNow();
        var answers = await db.AnswerRecords.Where(a => a.AttemptId == id).ToListAsync(ct);

        if (attempt.FinishedAt is null)
        {
            foreach (var pending in answers.Where(a => !a.Confirmed))
            {
                pending.Confirmed = true;
                await BankUpdater.ApplyAsync(db, pending.QuestionId, pending.IsCorrect, now, ct);
            }

            attempt.FinishedAt = now;
            await db.SaveChangesAsync(ct);
        }

        var ids = attempt.Questions.Select(q => q.QuestionId).ToList();
        var questions = await db.Questions.Include(q => q.Options).Where(q => ids.Contains(q.Id)).ToDictionaryAsync(q => q.Id, ct);
        var byQuestion = answers.ToDictionary(a => a.QuestionId);

        var results = attempt.Questions.OrderBy(q => q.Position).Select(aq =>
        {
            var q = questions[aq.QuestionId];
            byQuestion.TryGetValue(aq.QuestionId, out var answer);
            return new
            {
                QuestionId = q.Id,
                q.Prompt,
                Answered = answer is not null,
                IsCorrect = answer?.IsCorrect,
                Given = answer is null ? (JsonElement?)null : JsonDocument.Parse(answer.GivenAnswerJson).RootElement,
                q.Explanation,
                Correct = QuestionViews.CorrectAnswer(q),
            };
        }).ToList();

        var total = results.Count;
        var correct = results.Count(r => r.IsCorrect == true);
        return Results.Ok(new
        {
            AttemptId = id,
            attempt.Form,
            attempt.FinishedAt,
            Total = total,
            Answered = results.Count(r => r.Answered),
            Correct = correct,
            Percent = total == 0 ? 0 : (int)Math.Round(correct * 100.0 / total),
            Results = results,
        });
    }
}

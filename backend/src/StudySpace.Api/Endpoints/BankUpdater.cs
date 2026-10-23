using StudySpace.Domain.Bank;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Endpoints;

/// Applies one confirmed answer to the wrong-answer bank (REQ-BANK-001, REQ-BANK-002) using the domain rules.
public static class BankUpdater
{
    public static async Task ApplyAsync(StudySpaceDbContext db, int questionId, bool isCorrect, DateTimeOffset now, CancellationToken ct)
    {
        var item = await db.BankItems.FindAsync([questionId], ct);
        if (item is null)
        {
            if (!isCorrect)
            {
                db.BankItems.Add(new BankItemEntity { QuestionId = questionId, Status = BankStatus.Active, ConsecutiveCorrect = 0, LastWrongAt = now });
            }

            return;
        }

        var next = new BankEntry(questionId.ToString(), item.Status, item.ConsecutiveCorrect).RecordAnswer(isCorrect);
        item.Status = next.Status;
        item.ConsecutiveCorrect = next.ConsecutiveCorrect;
        if (!isCorrect)
        {
            item.LastWrongAt = now;
        }
    }
}

namespace StudySpace.Domain.Bank;

public enum BankStatus
{
    Active,
    Mastered,
}

/// A missed question in the wrong-answer bank (REQ-BANK-001, REQ-BANK-002).
/// Five correct answers in a row master it. A wrong answer resets the streak and
/// makes it active again, even if it was mastered.
public sealed record BankEntry(string QuestionId, BankStatus Status, int ConsecutiveCorrect)
{
    public const int MasteryStreak = 5;

    public static BankEntry EnterAfterWrong(string questionId) =>
        new(questionId, BankStatus.Active, 0);

    public BankEntry RecordAnswer(bool isCorrect)
    {
        if (!isCorrect)
        {
            return this with { Status = BankStatus.Active, ConsecutiveCorrect = 0 };
        }

        var streak = ConsecutiveCorrect + 1;
        var status = Status == BankStatus.Mastered || streak >= MasteryStreak
            ? BankStatus.Mastered
            : BankStatus.Active;

        return this with { Status = status, ConsecutiveCorrect = streak };
    }
}

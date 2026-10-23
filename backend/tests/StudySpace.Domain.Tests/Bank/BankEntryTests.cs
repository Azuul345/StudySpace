using StudySpace.Domain.Bank;

namespace StudySpace.Domain.Tests.Bank;

[Trait("Req", "REQ-BANK-001")]
public class BankEntryTests
{
    private static BankEntry Answer(BankEntry entry, bool correct, int times)
    {
        for (var i = 0; i < times; i++)
        {
            entry = entry.RecordAnswer(correct);
        }

        return entry;
    }

    [Fact]
    public void EnterAfterWrong_StartsActiveWithZeroStreak()
    {
        var entry = BankEntry.EnterAfterWrong("q1");

        Assert.Equal(BankStatus.Active, entry.Status);
        Assert.Equal(0, entry.ConsecutiveCorrect);
    }

    [Fact]
    public void RecordAnswer_FourCorrectInRow_StaysActive()
    {
        var entry = Answer(BankEntry.EnterAfterWrong("q1"), correct: true, times: 4);

        Assert.Equal(BankStatus.Active, entry.Status);
        Assert.Equal(4, entry.ConsecutiveCorrect);
    }

    [Fact]
    public void RecordAnswer_FifthCorrectInRow_MarksMastered()
    {
        var entry = Answer(BankEntry.EnterAfterWrong("q1"), correct: true, times: 5);

        Assert.Equal(BankStatus.Mastered, entry.Status);
    }

    [Fact]
    public void RecordAnswer_WrongAfterStreak_ResetsStreakToZero()
    {
        var entry = Answer(BankEntry.EnterAfterWrong("q1"), correct: true, times: 3)
            .RecordAnswer(false);

        Assert.Equal(BankStatus.Active, entry.Status);
        Assert.Equal(0, entry.ConsecutiveCorrect);
    }

    [Fact]
    public void RecordAnswer_WrongAfterMastered_ReturnsToActive()
    {
        var mastered = Answer(BankEntry.EnterAfterWrong("q1"), correct: true, times: 5);

        var entry = mastered.RecordAnswer(false);

        Assert.Equal(BankStatus.Active, entry.Status);
        Assert.Equal(0, entry.ConsecutiveCorrect);
    }

    [Fact]
    public void RecordAnswer_CorrectAfterMastered_StaysMastered()
    {
        var mastered = Answer(BankEntry.EnterAfterWrong("q1"), correct: true, times: 5);

        var entry = mastered.RecordAnswer(true);

        Assert.Equal(BankStatus.Mastered, entry.Status);
    }
}

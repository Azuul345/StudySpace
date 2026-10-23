using StudySpace.Domain.Scheduling;

namespace StudySpace.Domain.Tests.Scheduling;

[Trait("Req", "REQ-FC-001")]
public class Sm2SchedulerTests
{
    private static readonly Sm2Card NewCard = new(Repetitions: 0, IntervalDays: 0, EaseFactor: 2.5);

    [Fact]
    public void Review_FirstCorrectReview_SchedulesOneDay()
    {
        var card = Sm2Scheduler.Review(NewCard, quality: 4);

        Assert.Equal(1, card.Repetitions);
        Assert.Equal(1, card.IntervalDays);
    }

    [Fact]
    public void Review_SecondCorrectReview_SchedulesSixDays()
    {
        var card = Sm2Scheduler.Review(Sm2Scheduler.Review(NewCard, 4), 4);

        Assert.Equal(2, card.Repetitions);
        Assert.Equal(6, card.IntervalDays);
    }

    [Fact]
    public void Review_LaterCorrectReview_GrowsIntervalByEase()
    {
        var card = Sm2Scheduler.Review(Sm2Scheduler.Review(Sm2Scheduler.Review(NewCard, 4), 4), 4);

        // 6 days * 2.5 ease = 15 days
        Assert.Equal(15, card.IntervalDays);
    }

    [Fact]
    public void Review_FailedReview_ResetsRepetitionsAndInterval()
    {
        var card = Sm2Scheduler.Review(new Sm2Card(Repetitions: 3, IntervalDays: 15, EaseFactor: 2.5), quality: 2);

        Assert.Equal(0, card.Repetitions);
        Assert.Equal(1, card.IntervalDays);
    }

    [Fact]
    public void Review_PerfectRecall_RaisesEaseByPointOne()
    {
        var card = Sm2Scheduler.Review(new Sm2Card(0, 0, 2.5), quality: 5);

        Assert.Equal(2.6, card.EaseFactor, precision: 6);
    }

    [Fact]
    public void Review_QualityFour_LeavesEaseUnchanged()
    {
        var card = Sm2Scheduler.Review(new Sm2Card(0, 0, 2.5), quality: 4);

        Assert.Equal(2.5, card.EaseFactor, precision: 6);
    }

    [Fact]
    public void Review_RepeatedFailures_NeverDropEaseBelowMinimum()
    {
        var card = Sm2Scheduler.Review(new Sm2Card(0, 0, 1.3), quality: 0);

        Assert.Equal(1.3, card.EaseFactor, precision: 6);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Review_QualityOutsideZeroToFive_Throws(int quality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sm2Scheduler.Review(NewCard, quality));
    }
}

using StudySpace.Domain.Progress;

namespace StudySpace.Domain.Tests.Progress;

[Trait("Req", "REQ-PROG-001")]
public class MasteryCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_NoAnswers_IsUnseen()
    {
        var result = MasteryCalculator.Calculate([], Now);

        Assert.Equal(MasteryLevel.Unseen, result.Level);
        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void Calculate_AllRecentCorrect_IsStrongWithScoreOne()
    {
        var answers = new[] { new AnswerEvent(Now, true), new AnswerEvent(Now, true) };

        var result = MasteryCalculator.Calculate(answers, Now);

        Assert.Equal(1.0, result.Score, precision: 6);
        Assert.Equal(MasteryLevel.Strong, result.Level);
    }

    [Fact]
    public void Calculate_AllRecentWrong_IsWeakWithScoreZero()
    {
        var answers = new[] { new AnswerEvent(Now, false), new AnswerEvent(Now, false) };

        var result = MasteryCalculator.Calculate(answers, Now);

        Assert.Equal(0.0, result.Score, precision: 6);
        Assert.Equal(MasteryLevel.Weak, result.Level);
    }

    [Fact]
    public void Calculate_OlderAnswersCountLessThanNewer()
    {
        var recentCorrect = new[]
        {
            new AnswerEvent(Now, true),
            new AnswerEvent(Now.AddDays(-28), false),
        };
        var recentWrong = new[]
        {
            new AnswerEvent(Now, false),
            new AnswerEvent(Now.AddDays(-28), true),
        };

        var better = MasteryCalculator.Calculate(recentCorrect, Now).Score;
        var worse = MasteryCalculator.Calculate(recentWrong, Now).Score;

        Assert.True(better > worse);
    }

    [Fact]
    public void Calculate_AnswerFourteenDaysOld_CountsHalf()
    {
        // Correct 14 days ago (weight 0.5) and wrong today (weight 1.0): 0.5 / 1.5 = 1/3.
        var answers = new[]
        {
            new AnswerEvent(Now.AddDays(-14), true),
            new AnswerEvent(Now, false),
        };

        var result = MasteryCalculator.Calculate(answers, Now);

        Assert.Equal(1.0 / 3.0, result.Score, precision: 6);
    }

    [Fact]
    public void Calculate_ScoreAtEightyPercent_IsStrong()
    {
        var answers = new[]
        {
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, false),
        };

        var result = MasteryCalculator.Calculate(answers, Now);

        Assert.Equal(MasteryLevel.Strong, result.Level);
    }

    [Fact]
    public void Calculate_ScoreBelowEightyPercent_IsWeak()
    {
        var answers = new[]
        {
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, true),
            new AnswerEvent(Now, false),
        };

        var result = MasteryCalculator.Calculate(answers, Now);

        Assert.Equal(MasteryLevel.Weak, result.Level);
    }

    [Fact]
    public void ReviewWeight_WeakTopicWeighsMoreThanStrongTopic()
    {
        var weak = MasteryCalculator.Calculate([new AnswerEvent(Now, false)], Now);
        var strong = MasteryCalculator.Calculate([new AnswerEvent(Now, true)], Now);

        Assert.True(MasteryCalculator.ReviewWeight(weak) > MasteryCalculator.ReviewWeight(strong));
    }

    [Fact]
    public void ReviewWeight_Unseen_IsTwo()
    {
        var unseen = MasteryCalculator.Calculate([], Now);

        Assert.Equal(2.0, MasteryCalculator.ReviewWeight(unseen));
    }
}

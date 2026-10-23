namespace StudySpace.Domain.Progress;

public sealed record AnswerEvent(DateTimeOffset AnsweredAt, bool IsCorrect);

public enum MasteryLevel
{
    Unseen,
    Weak,
    Strong,
}

public sealed record MasteryResult(double Score, MasteryLevel Level);

/// Mastery per topic: recent accuracy with older answers counting less (REQ-PROG-001).
/// Each answer's weight halves every HalfLifeDays. Weak topics get more review (REQ-PROG-002).
public static class MasteryCalculator
{
    public const double HalfLifeDays = 14;
    public const double StrongThreshold = 0.8;

    public static MasteryResult Calculate(IEnumerable<AnswerEvent> answers, DateTimeOffset now)
    {
        var weightedCorrect = 0.0;
        var totalWeight = 0.0;
        var count = 0;

        foreach (var answer in answers)
        {
            count++;
            var ageDays = Math.Max(0, (now - answer.AnsweredAt).TotalDays);
            var weight = Math.Pow(0.5, ageDays / HalfLifeDays);

            totalWeight += weight;
            if (answer.IsCorrect)
            {
                weightedCorrect += weight;
            }
        }

        if (count == 0)
        {
            return new MasteryResult(0.0, MasteryLevel.Unseen);
        }

        var score = weightedCorrect / totalWeight;
        var level = score >= StrongThreshold ? MasteryLevel.Strong : MasteryLevel.Weak;
        return new MasteryResult(score, level);
    }

    /// Unseen topics get the same weight as a topic with score 0. Weight rises as score falls.
    public static double ReviewWeight(MasteryResult result) =>
        result.Level == MasteryLevel.Unseen ? 2.0 : 1.0 + (1.0 - result.Score);
}

namespace StudySpace.Domain.Scheduling;

/// SM-2 spaced repetition (REQ-FC-001). Quality is 0 to 5, where 3 or more is a pass.
public static class Sm2Scheduler
{
    private const double MinimumEase = 1.3;

    public static Sm2Card Review(Sm2Card card, int quality)
    {
        if (quality is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), quality, "Quality must be between 0 and 5.");
        }

        var penalty = 5 - quality;
        var ease = Math.Max(MinimumEase, card.EaseFactor + (0.1 - penalty * (0.08 + penalty * 0.02)));

        if (quality < 3)
        {
            return new Sm2Card(Repetitions: 0, IntervalDays: 1, EaseFactor: ease);
        }

        var repetitions = card.Repetitions + 1;
        var interval = repetitions switch
        {
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(card.IntervalDays * ease),
        };

        return new Sm2Card(repetitions, interval, ease);
    }
}

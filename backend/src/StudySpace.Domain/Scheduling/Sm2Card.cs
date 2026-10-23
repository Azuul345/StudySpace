namespace StudySpace.Domain.Scheduling;

/// Scheduling state for one flashcard (SM-2).
public sealed record Sm2Card(int Repetitions, int IntervalDays, double EaseFactor);

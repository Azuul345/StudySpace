namespace StudySpace.Application.Practice;

/// QuestionIds must already be in the order they should be asked.
public sealed record TopicPool(int TopicId, double ReviewWeight, IReadOnlyList<int> QuestionIds);

/// Builds the short daily drill (REQ-PRAC-004). Weak topics have a higher review weight, so they get a bigger share (REQ-PROG-002).
/// No randomness: the same pools always give the same drill.
public static class DailyDrillPlanner
{
    public static List<int> Plan(IReadOnlyList<TopicPool> pools, int count)
    {
        var usable = pools.Where(p => p.QuestionIds.Count > 0).OrderBy(p => p.TopicId).ToList();
        var remaining = Math.Min(count, usable.Sum(p => p.QuestionIds.Count));
        var take = usable.ToDictionary(p => p.TopicId, _ => 0);

        // Give out the shares, cap each topic at what it has, and hand the leftover to topics with room.
        while (remaining > 0)
        {
            var open = usable.Where(p => take[p.TopicId] < p.QuestionIds.Count).ToList();
            var shares = Allocate.LargestRemainder(open.Select(p => (p.TopicId, p.ReviewWeight)).ToList(), remaining);
            foreach (var (topicId, share) in shares)
            {
                var pool = open.First(p => p.TopicId == topicId);
                var add = Math.Min(share, pool.QuestionIds.Count - take[topicId]);
                take[topicId] += add;
                remaining -= add;
            }
        }

        return usable
            .OrderByDescending(p => p.ReviewWeight).ThenBy(p => p.TopicId)
            .SelectMany(p => p.QuestionIds.Take(take[p.TopicId]))
            .ToList();
    }
}

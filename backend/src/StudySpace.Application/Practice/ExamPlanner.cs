namespace StudySpace.Application.Practice;

public sealed record PlanCandidate(int QuestionId, int DomainId);

/// Picks questions for a timed exam in proportion to the official domain weights (REQ-PRAC-001).
/// The same seed always gives the same exam, which keeps it testable.
public static class ExamPlanner
{
    public static List<int> Pick(IReadOnlyList<PlanCandidate> pool, IReadOnlyDictionary<int, int> domainWeights, int count, int seed)
    {
        var random = new Random(seed);
        var byDomain = pool
            .GroupBy(c => c.DomainId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.QuestionId).OrderBy(id => id).OrderBy(_ => random.Next()).ToList());

        var quotas = Allocate.LargestRemainder(
            domainWeights.Keys.OrderBy(k => k).Select(k => (Key: k, Weight: (double)domainWeights[k])).ToList(), count);

        var picked = new List<int>();
        foreach (var (domain, quota) in quotas)
        {
            if (byDomain.TryGetValue(domain, out var list))
            {
                picked.AddRange(list.Take(quota));
            }
        }

        if (picked.Count < count)
        {
            var taken = picked.ToHashSet();
            var rest = pool.Select(c => c.QuestionId).Where(id => !taken.Contains(id)).OrderBy(id => id).OrderBy(_ => random.Next());
            picked.AddRange(rest.Take(count - picked.Count));
        }

        return picked.OrderBy(_ => random.Next()).ToList();
    }
}

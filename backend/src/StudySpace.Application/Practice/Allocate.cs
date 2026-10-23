namespace StudySpace.Application.Practice;

internal static class Allocate
{
    /// Splits total into whole shares in proportion to the weights. Ties go to the earlier key, so the result is stable.
    public static List<(int Key, int Count)> LargestRemainder(IReadOnlyList<(int Key, double Weight)> items, int total)
    {
        var sum = items.Sum(i => i.Weight);
        if (items.Count == 0 || sum <= 0 || total <= 0)
        {
            return items.Select(i => (i.Key, 0)).ToList();
        }

        var shares = items.Select(i => (i.Key, Exact: total * i.Weight / sum)).ToList();
        var counts = shares.Select(s => (s.Key, Count: (int)Math.Floor(s.Exact), Remainder: s.Exact - Math.Floor(s.Exact))).ToList();
        var left = total - counts.Sum(c => c.Count);

        var bumped = counts
            .Select((c, index) => (c, index))
            .OrderByDescending(x => x.c.Remainder).ThenBy(x => x.index)
            .Take(left)
            .Select(x => x.index)
            .ToHashSet();

        return counts.Select((c, index) => (c.Key, c.Count + (bumped.Contains(index) ? 1 : 0))).ToList();
    }
}

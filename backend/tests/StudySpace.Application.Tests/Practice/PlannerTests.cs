using StudySpace.Application.Practice;

namespace StudySpace.Application.Tests.Practice;

public class PlannerTests
{
    private static List<PlanCandidate> Candidates(int domain, int count, int startId) =>
        Enumerable.Range(startId, count).Select(i => new PlanCandidate(i, domain)).ToList();

    [Trait("Req", "REQ-PRAC-001")]
    [Fact]
    public void Exam_FollowsDomainWeights()
    {
        var pool = Candidates(1, 100, 1000).Concat(Candidates(2, 100, 2000)).Concat(Candidates(3, 100, 3000)).Concat(Candidates(4, 100, 4000)).ToList();
        var weights = new Dictionary<int, int> { [1] = 30, [2] = 26, [3] = 24, [4] = 20 };

        var picked = ExamPlanner.Pick(pool, weights, 65, seed: 7);

        Assert.Equal(65, picked.Count);
        Assert.Equal(65, picked.Distinct().Count());
        int Count(int domain) => picked.Count(id => pool.First(c => c.QuestionId == id).DomainId == domain);
        Assert.Equal(19, Count(1));
        Assert.Equal(17, Count(2));
        Assert.Equal(16, Count(3));
        Assert.Equal(13, Count(4));
    }

    [Fact]
    public void Exam_SameSeedGivesSameQuestions_DifferentSeedDiffers()
    {
        var pool = Candidates(1, 100, 1).Concat(Candidates(2, 100, 1000)).ToList();
        var weights = new Dictionary<int, int> { [1] = 50, [2] = 50 };

        var a = ExamPlanner.Pick(pool, weights, 20, seed: 1);
        var b = ExamPlanner.Pick(pool, weights, 20, seed: 1);
        var c = ExamPlanner.Pick(pool, weights, 20, seed: 2);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Exam_ShortDomainIsFilledFromOthers()
    {
        var pool = Candidates(1, 3, 1).Concat(Candidates(2, 100, 1000)).ToList();
        var weights = new Dictionary<int, int> { [1] = 50, [2] = 50 };

        var picked = ExamPlanner.Pick(pool, weights, 20, seed: 1);

        Assert.Equal(20, picked.Count);
        Assert.Equal(3, picked.Count(id => id < 1000));
    }

    [Fact]
    public void Exam_FewerQuestionsThanRequested_ReturnsAll()
    {
        var pool = Candidates(1, 5, 1);

        Assert.Equal(5, ExamPlanner.Pick(pool, new Dictionary<int, int> { [1] = 100 }, 65, seed: 1).Count);
    }

    [Fact]
    public void Exam_EmptyPool_ReturnsNothing()
    {
        Assert.Empty(ExamPlanner.Pick([], new Dictionary<int, int> { [1] = 100 }, 65, seed: 1));
    }

    [Trait("Req", "REQ-PRAC-004")]
    [Trait("Req", "REQ-PROG-002")]
    [Fact]
    public void Daily_WeakTopicGetsMoreQuestionsThanStrongTopic()
    {
        var pools = new List<TopicPool>
        {
            new(1, 1.9, Enumerable.Range(100, 20).ToList()),
            new(2, 1.1, Enumerable.Range(200, 20).ToList()),
        };

        var picked = DailyDrillPlanner.Plan(pools, 12);

        Assert.Equal(12, picked.Count);
        Assert.True(picked.Count(id => id < 200) > picked.Count(id => id >= 200));
    }

    [Fact]
    public void Daily_IsDeterministic()
    {
        var pools = new List<TopicPool>
        {
            new(1, 1.5, Enumerable.Range(100, 20).ToList()),
            new(2, 1.5, Enumerable.Range(200, 20).ToList()),
            new(3, 1.2, Enumerable.Range(300, 20).ToList()),
        };

        Assert.Equal(DailyDrillPlanner.Plan(pools, 13), DailyDrillPlanner.Plan(pools, 13));
    }

    [Fact]
    public void Daily_SmallTopicIsCappedAndRemainderGoesElsewhere()
    {
        var pools = new List<TopicPool>
        {
            new(1, 2.0, [1, 2]),
            new(2, 1.0, Enumerable.Range(200, 30).ToList()),
        };

        var picked = DailyDrillPlanner.Plan(pools, 10);

        Assert.Equal(10, picked.Count);
        Assert.Equal(2, picked.Count(id => id < 200));
    }

    [Fact]
    public void Daily_NotEnoughQuestions_ReturnsAllOfThem()
    {
        var pools = new List<TopicPool> { new(1, 1.0, [1, 2, 3]) };

        Assert.Equal(3, DailyDrillPlanner.Plan(pools, 12).Count);
    }
}

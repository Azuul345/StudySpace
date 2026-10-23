using StudySpace.Domain.Grading;

namespace StudySpace.Domain.Tests.Grading;

[Trait("Req", "REQ-Q-002")]
public class MultiResponseGraderTests
{
    [Fact]
    public void IsCorrect_SelectedSetEqualsCorrectSet_ReturnsTrue()
    {
        Assert.True(MultiResponseGrader.IsCorrect(["a", "c"], ["a", "c"]));
    }

    [Fact]
    public void IsCorrect_SameOptionsInDifferentOrder_ReturnsTrue()
    {
        Assert.True(MultiResponseGrader.IsCorrect(["c", "a"], ["a", "c"]));
    }

    [Fact]
    public void IsCorrect_MissingOneCorrectOption_ReturnsFalse()
    {
        Assert.False(MultiResponseGrader.IsCorrect(["a"], ["a", "c"]));
    }

    [Fact]
    public void IsCorrect_ExtraWrongOption_ReturnsFalse()
    {
        Assert.False(MultiResponseGrader.IsCorrect(["a", "b", "c"], ["a", "c"]));
    }

    [Fact]
    public void IsCorrect_EmptySelection_ReturnsFalse()
    {
        Assert.False(MultiResponseGrader.IsCorrect([], ["a"]));
    }
}

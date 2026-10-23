using StudySpace.Domain.Grading;

namespace StudySpace.Domain.Tests.Grading;

[Trait("Req", "REQ-Q-007")]
public class TypedAnswerGraderTests
{
    private static readonly RubricGroup[] Rubric =
    [
        new(["s3", "simple storage service"]),
        new(["encrypt", "encryption", "kms"]),
    ];

    [Fact]
    public void IsCorrect_AllGroupsMatchedIgnoringCaseAndPunctuation_ReturnsTrue()
    {
        Assert.True(TypedAnswerGrader.IsCorrect("Use S3, with KMS encryption.", Rubric));
    }

    [Fact]
    public void IsCorrect_SynonymMatchesGroup_ReturnsTrue()
    {
        Assert.True(TypedAnswerGrader.IsCorrect("Store it in Simple Storage Service and encrypt it", Rubric));
    }

    [Fact]
    public void IsCorrect_MissingGroup_ReturnsFalse()
    {
        Assert.False(TypedAnswerGrader.IsCorrect("Use S3 with versioning", Rubric));
    }

    [Fact]
    public void IsCorrect_TermInsideLongerWord_DoesNotMatch()
    {
        Assert.False(TypedAnswerGrader.IsCorrect("Use s33 and kmsx", Rubric));
    }

    [Fact]
    public void IsCorrect_EmptyAnswer_ReturnsFalse()
    {
        Assert.False(TypedAnswerGrader.IsCorrect("   ", Rubric));
    }

    [Fact]
    public void IsCorrect_EmptyRubric_Throws()
    {
        Assert.Throws<ArgumentException>(() => TypedAnswerGrader.IsCorrect("anything", []));
    }
}

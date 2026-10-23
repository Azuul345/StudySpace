using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

[Trait("Req", "REQ-SEC-002")]
public class PackPathGuardTests
{
    [Theory]
    [InlineData("pack.json")]
    [InlineData("topics/s3-security.json")]
    public void IsSafeRelativePath_NormalPath_ReturnsTrue(string path)
    {
        Assert.True(PackPathGuard.IsSafeRelativePath(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../secret.json")]
    [InlineData("topics/../../secret.json")]
    [InlineData("/etc/passwd")]
    [InlineData("C:secret.json")]
    [InlineData(@"topics\a.json")]
    [InlineData("topics//a.json")]
    [InlineData("topics/a.json\0")]
    public void IsSafeRelativePath_UnsafePath_ReturnsFalse(string path)
    {
        Assert.False(PackPathGuard.IsSafeRelativePath(path));
    }
}

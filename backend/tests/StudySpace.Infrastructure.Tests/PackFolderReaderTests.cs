using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.ContentPacks;

namespace StudySpace.Infrastructure.Tests;

[Trait("Req", "REQ-IMP-001")]
public class PackFolderReaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pack-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_dir, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    [Fact]
    public void Read_ReturnsFilesWithForwardSlashNames()
    {
        Write("pack.json", "{}");
        Write("topics/a.json", "{}");

        var (files, errors) = PackFolderReader.Read(_dir);

        Assert.Empty(errors);
        Assert.Equal(["pack.json", "topics/a.json"], files.Select(f => f.Name).Order());
    }

    [Fact]
    public void Read_FileOverTheLimit_IsReportedAndNotLoaded()
    {
        Write("pack.json", new string('x', PackValidator.MaxFileBytes + 1));

        var (files, errors) = PackFolderReader.Read(_dir);

        Assert.Empty(files);
        Assert.Contains(errors, e => e.File == "pack.json");
    }

    [Fact]
    public void Read_MissingFolder_ReturnsAnError()
    {
        var (files, errors) = PackFolderReader.Read(Path.Combine(_dir, "nope"));

        Assert.Empty(files);
        Assert.Single(errors);
    }

    [Fact]
    public async Task ReadThenImport_RealFolder_Works()
    {
        Write("pack.json", SamplePack.Manifest);
        Write("topics/t1.json", SamplePack.Topic);
        using var db = new TestDb();
        var (files, _) = PackFolderReader.Read(_dir);
        await using var context = db.NewContext();

        var result = await new PackImporter(new StudySpace.Infrastructure.Persistence.EfPackRepository(context)).ImportAsync(files, CancellationToken.None);

        Assert.Equal(ImportOutcome.Imported, result.Outcome);
    }
}

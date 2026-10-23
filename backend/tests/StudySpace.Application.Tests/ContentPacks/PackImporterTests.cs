using StudySpace.Application.ContentPacks;

namespace StudySpace.Application.Tests.ContentPacks;

[Trait("Req", "REQ-IMP-002")]
public class PackImporterTests
{
    private sealed class FakeRepository : IPackRepository
    {
        public List<(ParsedPack Pack, string Hash)> Saved { get; } = [];

        public Task<bool> ExistsAsync(string packId, string version, CancellationToken ct) =>
            Task.FromResult(Saved.Any(s => s.Pack.PackId == packId && s.Pack.Version == version));

        public Task SaveAsync(ParsedPack pack, string fileHash, CancellationToken ct)
        {
            Saved.Add((pack, fileHash));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Import_ValidPack_SavesIt()
    {
        var repo = new FakeRepository();

        var result = await new PackImporter(repo).ImportAsync(PackSamples.ValidPack(), CancellationToken.None);

        Assert.Equal(ImportOutcome.Imported, result.Outcome);
        Assert.Single(repo.Saved);
        Assert.Equal(64, repo.Saved[0].Hash.Length);
    }

    [Fact]
    public async Task Import_SameVersionTwice_IsANoOp()
    {
        var repo = new FakeRepository();
        var importer = new PackImporter(repo);
        await importer.ImportAsync(PackSamples.ValidPack(), CancellationToken.None);

        var second = await importer.ImportAsync(PackSamples.ValidPack(), CancellationToken.None);

        Assert.Equal(ImportOutcome.AlreadyImported, second.Outcome);
        Assert.Single(repo.Saved);
    }

    [Fact]
    public async Task Import_InvalidPack_IsRejectedWithErrorsAndSavesNothing()
    {
        var repo = new FakeRepository();
        var files = PackSamples.ValidPack();
        files[0] = new("pack.json", PackSamples.Replace(PackSamples.Manifest, "\"weightPercent\": 70", "\"weightPercent\": 10"));

        var result = await new PackImporter(repo).ImportAsync(files, CancellationToken.None);

        Assert.Equal(ImportOutcome.Invalid, result.Outcome);
        Assert.NotEmpty(result.Errors);
        Assert.Empty(repo.Saved);
    }

    [Fact]
    public void Hash_IsStableAndIgnoresFileOrder()
    {
        var files = PackSamples.ValidPack();
        var reversed = files.AsEnumerable().Reverse().ToList();

        Assert.Equal(PackHasher.Hash(files), PackHasher.Hash(reversed));
    }

    [Fact]
    public void Hash_ChangesWhenContentChanges()
    {
        var files = PackSamples.ValidPack();
        var changed = PackSamples.ValidPack();
        changed[2] = new("flashcards.json", PackSamples.Flashcards.Replace("KMS", "WAF", StringComparison.Ordinal));

        Assert.NotEqual(PackHasher.Hash(files), PackHasher.Hash(changed));
    }
}

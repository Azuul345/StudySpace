namespace StudySpace.Application.ContentPacks;

public enum ImportOutcome { Imported, AlreadyImported, Invalid }

public sealed record ImportResult(ImportOutcome Outcome, IReadOnlyList<ValidationError> Errors);

/// Validate, then import (REQ-IMP-001, REQ-IMP-002). Content is untrusted until it passes the validator.
public sealed class PackImporter(IPackRepository repository)
{
    public async Task<ImportResult> ImportAsync(IReadOnlyList<PackFile> files, CancellationToken ct)
    {
        var validation = PackValidator.Validate(files);
        if (!validation.IsValid)
        {
            return new ImportResult(ImportOutcome.Invalid, validation.Errors);
        }

        var pack = PackParser.Parse(files);
        if (await repository.ExistsAsync(pack.PackId, pack.Version, ct))
        {
            return new ImportResult(ImportOutcome.AlreadyImported, []);
        }

        await repository.SaveAsync(pack, PackHasher.Hash(files), ct);
        return new ImportResult(ImportOutcome.Imported, []);
    }
}

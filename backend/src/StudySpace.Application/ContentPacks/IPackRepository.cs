namespace StudySpace.Application.ContentPacks;

public interface IPackRepository
{
    Task<bool> ExistsAsync(string packId, string version, CancellationToken ct);

    /// Saves the whole pack in one transaction: all of it, or none of it.
    Task SaveAsync(ParsedPack pack, string fileHash, CancellationToken ct);
}

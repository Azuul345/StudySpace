using System.Security.Cryptography;
using System.Text;

namespace StudySpace.Application.ContentPacks;

public static class PackHasher
{
    /// SHA-256 over every file's name and content, sorted by name so file order does not matter.
    public static string Hash(IEnumerable<PackFile> files)
    {
        using var sha = SHA256.Create();
        foreach (var file in files.OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            var bytes = Encoding.UTF8.GetBytes($"{file.Name}\n{file.Content}\n");
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }
}

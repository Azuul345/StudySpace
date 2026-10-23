using StudySpace.Application.ContentPacks;

namespace StudySpace.Infrastructure.ContentPacks;

/// Reads a pack folder from disk. Only regular .json files are loaded, and a file over the size limit
/// is reported without being read. Links and junctions are skipped so a pack cannot point outside its folder.
public static class PackFolderReader
{
    public static (List<PackFile> Files, List<ValidationError> Errors) Read(string folder)
    {
        var files = new List<PackFile>();
        var errors = new List<ValidationError>();

        if (!Directory.Exists(folder))
        {
            errors.Add(new ValidationError(folder, string.Empty, "Pack folder not found."));
            return (files, errors);
        }

        var root = new DirectoryInfo(folder);
        foreach (var info in root.EnumerateFiles("*.json", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(root.FullName, info.FullName).Replace(Path.DirectorySeparatorChar, '/');
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint) || info.LinkTarget is not null)
            {
                errors.Add(new ValidationError(name, string.Empty, "Links are not allowed in a pack."));
            }
            else if (info.Length > PackValidator.MaxFileBytes)
            {
                errors.Add(new ValidationError(name, string.Empty, $"File is larger than {PackValidator.MaxFileBytes} bytes."));
            }
            else
            {
                files.Add(new PackFile(name, File.ReadAllText(info.FullName)));
            }
        }

        return (files, errors);
    }
}

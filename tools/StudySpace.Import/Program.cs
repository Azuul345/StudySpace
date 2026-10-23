using Microsoft.EntityFrameworkCore;
using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.ContentPacks;
using StudySpace.Infrastructure.Persistence;

const string Usage = "Usage:\n  StudySpace.Import validate <pack-folder>\n  StudySpace.Import import <pack-folder> [--db <path>]";

if (args.Length < 2 || args[0] is not ("validate" or "import"))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var (files, readErrors) = PackFolderReader.Read(args[1]);
if (readErrors.Count > 0)
{
    return Report(readErrors);
}

if (args[0] == "validate")
{
    var validation = PackValidator.Validate(files);
    if (!validation.IsValid)
    {
        return Report(validation.Errors);
    }

    Console.WriteLine("Pack is valid.");
    return 0;
}

var dbIndex = Array.IndexOf(args, "--db");
var dbPath = dbIndex >= 0 && dbIndex + 1 < args.Length ? args[dbIndex + 1] : "studyspace.db";

await using var context = new StudySpaceDbContext(new DbContextOptionsBuilder<StudySpaceDbContext>().UseSqlite($"Data Source={dbPath}").Options);
await context.Database.MigrateAsync();

var result = await new PackImporter(new EfPackRepository(context)).ImportAsync(files, CancellationToken.None);
switch (result.Outcome)
{
    case ImportOutcome.Imported:
        Console.WriteLine("Imported.");
        return 0;
    case ImportOutcome.AlreadyImported:
        Console.WriteLine("This pack version is already imported. Nothing changed.");
        return 0;
    default:
        return Report(result.Errors);
}

static int Report(IEnumerable<ValidationError> errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"{error.File} {error.Path}: {error.Message}");
    }

    return 1;
}

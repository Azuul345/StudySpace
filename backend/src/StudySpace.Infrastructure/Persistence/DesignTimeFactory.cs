using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudySpace.Infrastructure.Persistence;

/// Used only by "dotnet ef" to create migrations. The path is never read at runtime.
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<StudySpaceDbContext>
{
    public StudySpaceDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<StudySpaceDbContext>().UseSqlite("Data Source=design-time.db").Options);
}

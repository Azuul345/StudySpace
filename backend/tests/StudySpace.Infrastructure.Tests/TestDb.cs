using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Infrastructure.Tests;

/// One in-memory SQLite database per test, built by the real migrations.
internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDb()
    {
        _connection.Open();
        using var context = NewContext();
        context.Database.Migrate();
    }

    public StudySpaceDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StudySpaceDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

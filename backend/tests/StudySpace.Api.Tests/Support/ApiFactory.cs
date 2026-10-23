using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudySpace.Application.ContentPacks;
using StudySpace.Infrastructure.Persistence;

namespace StudySpace.Api.Tests.Support;

/// Starts the real API in memory against a private in-memory SQLite database, and lets tests control the clock.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestClock Clock { get; } = new(new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<StudySpaceDbContext>>();
            services.AddDbContext<StudySpaceDbContext>(o => o.UseSqlite(_connection));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    internal async Task SeedAsync(IReadOnlyList<PackFile> files)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StudySpaceDbContext>();
        var result = await new PackImporter(new EfPackRepository(context)).ImportAsync(files, CancellationToken.None);
        Assert.Equal(ImportOutcome.Imported, result.Outcome);
    }

    internal async Task<T> QueryAsync<T>(Func<StudySpaceDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<StudySpaceDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public sealed class TestClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

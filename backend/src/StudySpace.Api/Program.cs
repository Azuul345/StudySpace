using Microsoft.EntityFrameworkCore;
using StudySpace.Api;
using StudySpace.Api.Endpoints;
using StudySpace.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(ListenAddress.Resolve(builder.Configuration).ToArray());
builder.Services.AddDbContext<StudySpaceDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=studyspace.db"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<StudySpaceDbContext>().Database.Migrate();
}

app.UseProblemErrors();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

var api = app.MapGroup("/api/v1");
api.MapCatalog();
api.MapAttempts();

app.Run();

public partial class Program;

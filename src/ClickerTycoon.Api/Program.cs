using ClickerTycoon.Api.Middleware;
using ClickerTycoon.Application;
using ClickerTycoon.Infrastructure;
using ClickerTycoon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "clickertycoon.db");
var connectionString = $"Data Source={dbPath}";
var gameConfigPath = Path.Combine(builder.Environment.ContentRootPath, "gameconfig.json");

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, gameConfigPath);

var app = builder.Build();

// The whole point of using EnsureCreated (instead of migrations) is that a
// fresh clone runs with a single `dotnet run` - no `dotnet ef database update` step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Any non-API route falls back to index.html so the SPA-style frontend
// keeps working on refresh (there's no client-side router here, but this
// keeps the door open and is harmless for the single-page game screen).
app.MapFallbackToFile("index.html");

app.Run();

using Microsoft.EntityFrameworkCore;
using RevenuePilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/health", async (TimeProvider clock, AppDbContext db, CancellationToken ct) =>
{
    var dbOk = await db.Database.CanConnectAsync(ct);
    var body = new
    {
        status = dbOk ? "healthy" : "unhealthy",
        database = dbOk ? "connected" : "unreachable",
        utcNow = clock.GetUtcNow()
    };
    return dbOk ? Results.Ok(body) : Results.Json(body, statusCode: 503);
});

app.Run();
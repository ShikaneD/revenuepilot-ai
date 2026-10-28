var builder = WebApplication.CreateBuilder(args);

// Inject the clock instead of calling DateTime.UtcNow, so tests can fix the time.
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.MapGet("/health", (TimeProvider clock) =>
    Results.Ok(new { status = "healthy", utcNow = clock.GetUtcNow() }));

app.Run();
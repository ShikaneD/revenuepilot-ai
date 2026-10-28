using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Analytics;
using RevenuePilot.Infrastructure.Persistence;
using RevenuePilot.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// "dotnet run -- seed" or "dotnet run -- reseed" fills the database with demo data, then exits.
if (args.Contains("seed") || args.Contains("reseed"))
{
    using var scope = app.Services.CreateScope();
    var seedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedClock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    Console.WriteLine(await DemoSeeder.SeedAsync(seedDb, seedClock.GetUtcNow().UtcDateTime, args.Contains("reseed")));
    return;
}

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

// TEMPORARY: serves the demo business with no login. Phase 7 replaces this with
// authentication and a server-side check that the user belongs to the business.
app.MapGet("/api/v1/dashboard/summary", async (int? days, TimeProvider clock, AppDbContext db, CancellationToken ct) =>
{
    var n = days ?? 30;
    if (n is not (7 or 30 or 90))
        return Results.BadRequest(new { error = "days must be 7, 30 or 90" });

    var business = await db.Businesses.AsNoTracking()
        .Where(b => b.IsDemo).OrderBy(b => b.CreatedAt).FirstOrDefaultAsync(ct);
    if (business is null)
        return Results.NotFound(new { error = "No business found. Run: dotnet run -- seed" });

    var period = PeriodCalculator.LastDays(clock.GetUtcNow().UtcDateTime, business.TimeZoneId, n);
    var previous = period.Previous();

    var orders = await db.Orders.AsNoTracking()
        .Where(o => o.BusinessId == business.Id && o.OrderDate >= previous.StartUtc && o.OrderDate < period.EndUtc)
        .Select(o => new OrderRecord(o.CustomerId, o.OrderDate, o.Status, o.TotalAmount, o.RefundAmount))
        .ToListAsync(ct);

    var net = RevenueCalculator.NetRevenue(orders, period);
    var prevNet = RevenueCalculator.NetRevenue(orders, previous);

    return Results.Ok(new
    {
        business = new { business.Name, business.Currency, business.TimeZoneId, business.IsDemo },
        period = new { days = n, startUtc = period.StartUtc, endUtc = period.EndUtc },
        comparisonPeriod = new { startUtc = previous.StartUtc, endUtc = previous.EndUtc },
        netRevenue = net,
        previousNetRevenue = prevNet,
        growthPercent = RevenueCalculator.GrowthPercent(net, prevNet),
        orderCount = RevenueCalculator.OrderCount(orders, period),
        averageOrderValue = RevenueCalculator.AverageOrderValue(orders, period),
        refundsTotal = RevenueCalculator.RefundsTotal(orders, period),
        failedPaymentCount = RevenueCalculator.FailedPaymentCount(orders, period),
        definitions = new
        {
            netRevenue = "Sum of (order total minus refund) for Paid orders in the period. Failed, cancelled and pending orders are excluded.",
            growthPercent = "(current minus previous) / previous x 100, compared with the equivalent period immediately before. Null when the previous period is zero."
        }
    });
});

app.Run();
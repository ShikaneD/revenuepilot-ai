using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Analytics;
using RevenuePilot.Domain.Churn;
using RevenuePilot.Domain.Entities;
using RevenuePilot.Infrastructure.Insights;
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

// TEMPORARY: every endpoint below serves the demo business with no login.
// Phase 7 replaces this with authentication and a server-side membership check.
static async Task<Business?> DemoBusinessAsync(AppDbContext db, CancellationToken ct) =>
    await db.Businesses.AsNoTracking().Where(b => b.IsDemo).OrderBy(b => b.CreatedAt).FirstOrDefaultAsync(ct);

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

app.MapGet("/api/v1/dashboard/summary", async (int? days, TimeProvider clock, AppDbContext db, CancellationToken ct) =>
{
    var n = days ?? 30;
    if (n is not (7 or 30 or 90))
        return Results.BadRequest(new { error = "days must be 7, 30 or 90" });

    var business = await DemoBusinessAsync(db, ct);
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

app.MapGet("/api/v1/customers", async (string? search, string? sort, string? risk,
    TimeProvider clock, AppDbContext db, CancellationToken ct) =>
{
    var business = await DemoBusinessAsync(db, ct);
    if (business is null)
        return Results.NotFound(new { error = "No business found. Run: dotnet run -- seed" });

    IEnumerable<CustomerInsightDto> q =
        await CustomerInsightsService.LoadAsync(db, business.Id, clock.GetUtcNow().UtcDateTime, ct);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        q = q.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    var band = risk?.Trim().ToLowerInvariant();
    if (band is "low" or "medium" or "high")
        q = q.Where(c => c.Risk != null && c.Risk.Band.Equals(band, StringComparison.OrdinalIgnoreCase));
    else if (band == "none")
        q = q.Where(c => c.Risk == null);

    q = sort?.ToLowerInvariant() switch
    {
        "name" => q.OrderBy(c => c.Name),
        "risk" => q.OrderByDescending(c => c.Risk?.Score ?? -1),
        "last" => q.OrderByDescending(c => c.LastPurchaseUtc),
        _ => q.OrderByDescending(c => c.TotalRevenue)
    };

    var list = q.ToList();
    return Results.Ok(new
    {
        business = new { business.Name, business.Currency, business.TimeZoneId, business.IsDemo },
        count = list.Count,
        customers = list
    });
});

app.MapGet("/api/v1/risk", async (TimeProvider clock, AppDbContext db, CancellationToken ct) =>
{
    var business = await DemoBusinessAsync(db, ct);
    if (business is null)
        return Results.NotFound(new { error = "No business found. Run: dotnet run -- seed" });

    var all = await CustomerInsightsService.LoadAsync(db, business.Id, clock.GetUtcNow().UtcDateTime, ct);
    var flagged = all.Where(c => c.Risk is not null && c.Risk.Band != "Low")
        .OrderByDescending(c => c.Risk!.Score).ToList();
    var options = new ChurnOptions();

    return Results.Ok(new
    {
        business = new { business.Name, business.Currency, business.TimeZoneId, business.IsDemo },
        scoredCustomers = all.Count(c => c.Risk is not null),
        notScoredCustomers = all.Count(c => c.Risk is null),
        highCount = flagged.Count(c => c.Risk!.Band == "High"),
        mediumCount = flagged.Count(c => c.Risk!.Band == "Medium"),
        totalEstimatedExposure = flagged.Sum(c => c.Risk!.EstimatedExposure),
        bands = new { medium = options.MediumThreshold, high = options.HighThreshold },
        note = "Scores come from a rule-based risk indicator, not a validated prediction. Exposure is an estimate that assumes each customer keeps buying at their historical pace.",
        customers = flagged
    });
});

app.Run();
using RevenuePilot.Domain.Entities;

namespace RevenuePilot.Domain.Analytics;

/// <summary>Half-open UTC range: StartUtc inclusive, EndUtc exclusive.</summary>
public sealed record Period(DateTime StartUtc, DateTime EndUtc)
{
    public bool Contains(DateTime utc) => utc >= StartUtc && utc < EndUtc;
    public TimeSpan Length => EndUtc - StartUtc;
    /// <summary>The equivalent period immediately before this one.</summary>
    public Period Previous() => new(StartUtc - Length, StartUtc);
}

public sealed record OrderRecord(
    Guid CustomerId, DateTime OrderDateUtc, OrderStatus Status, decimal TotalAmount, decimal RefundAmount);

/// <summary>
/// Definitions: only orders with status Paid count. Net revenue = TotalAmount - RefundAmount.
/// Failed, Cancelled, Pending and fully Refunded orders are excluded from revenue.
/// </summary>
public static class RevenueCalculator
{
    private static IEnumerable<OrderRecord> Qualifying(IEnumerable<OrderRecord> orders, Period p) =>
        orders.Where(o => o.Status == OrderStatus.Paid && p.Contains(o.OrderDateUtc));

    public static decimal NetRevenue(IEnumerable<OrderRecord> orders, Period p) =>
        Qualifying(orders, p).Sum(o => o.TotalAmount - o.RefundAmount);

    public static int OrderCount(IEnumerable<OrderRecord> orders, Period p) =>
        Qualifying(orders, p).Count();

    public static decimal? AverageOrderValue(IEnumerable<OrderRecord> orders, Period p)
    {
        var count = OrderCount(orders, p);
        return count == 0 ? null : NetRevenue(orders, p) / count;
    }

    public static decimal RefundsTotal(IEnumerable<OrderRecord> orders, Period p) =>
        Qualifying(orders, p).Sum(o => o.RefundAmount);

    public static int FailedPaymentCount(IEnumerable<OrderRecord> orders, Period p) =>
        orders.Count(o => o.Status == OrderStatus.Failed && p.Contains(o.OrderDateUtc));

    /// <summary>Returns null (not applicable) when the previous period is zero.</summary>
    public static decimal? GrowthPercent(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((current - previous) / previous * 100m, 1);
}

public static class PeriodCalculator
{
    /// <summary>The last N whole days in the business's timezone, including today.</summary>
    public static Period LastDays(DateTime nowUtc, string timeZoneId, int days)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz).Date;
        var endLocal = DateTime.SpecifyKind(localToday.AddDays(1), DateTimeKind.Unspecified);
        var startLocal = DateTime.SpecifyKind(localToday.AddDays(1 - days), DateTimeKind.Unspecified);
        return new Period(TimeZoneInfo.ConvertTimeToUtc(startLocal, tz), TimeZoneInfo.ConvertTimeToUtc(endLocal, tz));
    }
}
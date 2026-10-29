using System.Globalization;
using RevenuePilot.Domain.Churn;

namespace RevenuePilot.Domain.Analytics;

/// <summary>A paid order reduced to what customer metrics need. Net = total minus refund.</summary>
public sealed record NetOrder(DateTime DateUtc, decimal Net);

public sealed record CustomerSummary(
    int PaidOrderCount, decimal TotalRevenue, decimal? AverageOrderValue, DateTime? LastPurchaseUtc);

public sealed record MonthlyPoint(string Month, decimal Revenue);

public static class CustomerMetrics
{
    public static CustomerSummary Summarise(IReadOnlyCollection<NetOrder> paidOrders)
    {
        if (paidOrders.Count == 0) return new CustomerSummary(0, 0m, null, null);
        var total = paidOrders.Sum(o => o.Net);
        return new CustomerSummary(paidOrders.Count, total, total / paidOrders.Count, paidOrders.Max(o => o.DateUtc));
    }

    /// <summary>
    /// Net revenue per calendar month in the business's timezone, for the last N months
    /// including the current one. Months with no orders are included with zero revenue.
    /// </summary>
    public static IReadOnlyList<MonthlyPoint> MonthlyRevenue(
        IEnumerable<NetOrder> paidOrders, DateTime nowUtc, string timeZoneId, int months)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), tz);
        var thisMonth = new DateTime(nowLocal.Year, nowLocal.Month, 1);

        var totals = new Dictionary<(int Year, int Month), decimal>();
        foreach (var o in paidOrders)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o.DateUtc, DateTimeKind.Utc), tz);
            var key = (local.Year, local.Month);
            totals[key] = totals.GetValueOrDefault(key) + o.Net;
        }

        var result = new List<MonthlyPoint>();
        for (var i = months - 1; i >= 0; i--)
        {
            var m = thisMonth.AddMonths(-i);
            result.Add(new MonthlyPoint(m.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                totals.GetValueOrDefault((m.Year, m.Month))));
        }
        return result;
    }

    public static string SuggestedAction(RiskBand band) => band switch
    {
        RiskBand.High => "Contact this customer personally this week to find out what changed.",
        RiskBand.Medium => "Send a friendly check-in and ask whether anything has changed.",
        _ => "No action needed. Keep serving this customer as usual."
    };
}
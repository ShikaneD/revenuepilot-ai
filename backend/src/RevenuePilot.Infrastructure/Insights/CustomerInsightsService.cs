using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Analytics;
using RevenuePilot.Domain.Churn;
using RevenuePilot.Domain.Entities;
using RevenuePilot.Infrastructure.Persistence;

namespace RevenuePilot.Infrastructure.Insights;

public sealed record RiskDto(
    int Score, string Band, IReadOnlyList<string> Evidence,
    decimal EstimatedExposure, int AverageGapDays, int DaysSinceLastPurchase, string SuggestedAction);

public sealed record CustomerInsightDto(
    Guid Id, string Name, string? Segment, string? Email,
    DateTime? LastPurchaseUtc, int PaidOrderCount, decimal TotalRevenue,
    decimal? AverageOrderValue, RiskDto? Risk);

public sealed record OrderHistoryDto(
    string ExternalOrderId, DateTime OrderDateUtc, string Status,
    decimal TotalAmount, decimal RefundAmount, decimal NetAmount);

public sealed record CustomerDetailDto(
    CustomerInsightDto Customer, IReadOnlyList<MonthlyPoint> MonthlyRevenue, IReadOnlyList<OrderHistoryDto> Orders);

public static class CustomerInsightsService
{
    public static async Task<List<CustomerInsightDto>> LoadAsync(
        AppDbContext db, Guid businessId, DateTime nowUtc, CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.Status == CustomerStatus.Active)
            .ToListAsync(ct);

        // Only Paid orders count. Net = total minus refund.
        var paid = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.Status == OrderStatus.Paid)
            .Select(o => new { o.CustomerId, o.OrderDate, Net = o.TotalAmount - o.RefundAmount })
            .ToListAsync(ct);

        var byCustomer = paid.GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => g.Select(o => new NetOrder(o.OrderDate, o.Net)).ToList());

        var result = new List<CustomerInsightDto>();
        foreach (var c in customers)
        {
            var orders = byCustomer.TryGetValue(c.Id, out var found) ? found : new List<NetOrder>();
            result.Add(Build(c, orders, nowUtc));
        }
        return result;
    }

    /// <summary>The customer must belong to the given business, otherwise null is returned.</summary>
    public static async Task<CustomerDetailDto?> GetDetailAsync(
        AppDbContext db, Business business, Guid customerId, DateTime nowUtc, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == business.Id, ct);
        if (customer is null) return null;

        var all = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == business.Id && o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);

        var paid = all.Where(o => o.Status == OrderStatus.Paid)
            .Select(o => new NetOrder(o.OrderDate, o.TotalAmount - o.RefundAmount))
            .ToList();

        var history = all.Select(o => new OrderHistoryDto(
            o.ExternalOrderId, o.OrderDate, o.Status.ToString(), o.TotalAmount, o.RefundAmount,
            o.Status == OrderStatus.Paid ? o.TotalAmount - o.RefundAmount : 0m)).ToList();

        return new CustomerDetailDto(
            Build(customer, paid, nowUtc),
            CustomerMetrics.MonthlyRevenue(paid, nowUtc, business.TimeZoneId, 12),
            history);
    }

    private static CustomerInsightDto Build(Customer c, IReadOnlyList<NetOrder> paid, DateTime nowUtc)
    {
        var summary = CustomerMetrics.Summarise(paid);
        var score = ChurnScorer.Score(paid.Select(o => new PaidTransaction(o.DateUtc, o.Net)), nowUtc);
        RiskDto? risk = score is null
            ? null
            : new RiskDto(score.Score, score.Band.ToString(), score.Evidence, score.EstimatedExposure,
                score.AverageGapDays, score.DaysSinceLastPurchase, CustomerMetrics.SuggestedAction(score.Band));

        return new CustomerInsightDto(
            c.Id, c.Name, c.Segment, c.Email,
            summary.LastPurchaseUtc, summary.PaidOrderCount, summary.TotalRevenue,
            summary.AverageOrderValue, risk);
    }
}
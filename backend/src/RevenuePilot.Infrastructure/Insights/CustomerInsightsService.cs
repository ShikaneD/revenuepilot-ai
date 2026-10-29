using Microsoft.EntityFrameworkCore;
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

public static class CustomerInsightsService
{
    private sealed record OrderRow(Guid CustomerId, DateTime OrderDate, decimal Net);

    public static async Task<List<CustomerInsightDto>> LoadAsync(
        AppDbContext db, Guid businessId, DateTime nowUtc, CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.Status == CustomerStatus.Active)
            .ToListAsync(ct);

        // Only Paid orders count. Net = total minus refund.
        var paid = await db.Orders.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.Status == OrderStatus.Paid)
            .Select(o => new OrderRow(o.CustomerId, o.OrderDate, o.TotalAmount - o.RefundAmount))
            .ToListAsync(ct);

        var byCustomer = paid.GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.OrderDate).ToList());

        var result = new List<CustomerInsightDto>();
        foreach (var c in customers)
        {
            var orders = byCustomer.TryGetValue(c.Id, out var found) ? found : new List<OrderRow>();
            var total = orders.Sum(o => o.Net);

            var score = ChurnScorer.Score(orders.Select(o => new PaidTransaction(o.OrderDate, o.Net)), nowUtc);
            RiskDto? risk = score is null
                ? null
                : new RiskDto(score.Score, score.Band.ToString(), score.Evidence, score.EstimatedExposure,
                    score.AverageGapDays, score.DaysSinceLastPurchase, SuggestedAction(score.Band));

            result.Add(new CustomerInsightDto(
                c.Id, c.Name, c.Segment, c.Email,
                orders.Count > 0 ? orders[^1].OrderDate : null,
                orders.Count, total,
                orders.Count > 0 ? total / orders.Count : null,
                risk));
        }
        return result;
    }

    private static string SuggestedAction(RiskBand band) => band switch
    {
        RiskBand.High => "Contact this customer personally this week to find out what changed.",
        RiskBand.Medium => "Send a friendly check-in and ask whether anything has changed.",
        _ => "No action needed. Keep serving this customer as usual."
    };
}
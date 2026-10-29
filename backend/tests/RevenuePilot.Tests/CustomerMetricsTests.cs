using RevenuePilot.Domain.Analytics;
using RevenuePilot.Domain.Churn;
using Xunit;

namespace RevenuePilot.Tests;

public class CustomerMetricsTests
{
    private static DateTime Utc(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Summarise_with_no_orders_returns_zero_and_nulls()
    {
        var s = CustomerMetrics.Summarise(Array.Empty<NetOrder>());
        Assert.Equal(0, s.PaidOrderCount);
        Assert.Equal(0m, s.TotalRevenue);
        Assert.Null(s.AverageOrderValue);
        Assert.Null(s.LastPurchaseUtc);
    }

    [Fact]
    public void Summarise_totals_averages_and_finds_the_latest_order_whatever_the_input_order()
    {
        var orders = new[]
        {
            new NetOrder(Utc(2026, 10, 1), 300m),
            new NetOrder(Utc(2026, 8, 1), 100m),
            new NetOrder(Utc(2026, 9, 1), 200m),
        };
        var s = CustomerMetrics.Summarise(orders);
        Assert.Equal(3, s.PaidOrderCount);
        Assert.Equal(600m, s.TotalRevenue);
        Assert.Equal(200m, s.AverageOrderValue);
        Assert.Equal(Utc(2026, 10, 1), s.LastPurchaseUtc);
    }

    [Fact]
    public void Monthly_revenue_zero_fills_months_and_uses_the_business_timezone()
    {
        var orders = new[]
        {
            new NetOrder(Utc(2026, 9, 30, 23), 100m), // 01:00 on 1 October in Johannesburg
            new NetOrder(Utc(2026, 8, 15, 10), 50m),
        };
        var result = CustomerMetrics.MonthlyRevenue(orders, Utc(2026, 10, 28, 12), "Africa/Johannesburg", 3);

        Assert.Equal(new[] { "2026-08", "2026-09", "2026-10" }, result.Select(p => p.Month).ToArray());
        Assert.Equal(new[] { 50m, 0m, 100m }, result.Select(p => p.Revenue).ToArray());
    }

    [Fact]
    public void Monthly_revenue_returns_exactly_the_requested_months_ending_this_month()
    {
        var result = CustomerMetrics.MonthlyRevenue(
            Array.Empty<NetOrder>(), Utc(2026, 1, 15), "Africa/Johannesburg", 12);

        Assert.Equal(12, result.Count);
        Assert.Equal("2025-02", result[0].Month);
        Assert.Equal("2026-01", result[^1].Month);
    }

    [Theory]
    [InlineData(RiskBand.High, "Contact")]
    [InlineData(RiskBand.Medium, "check-in")]
    [InlineData(RiskBand.Low, "No action needed")]
    public void Suggested_action_matches_the_risk_band(RiskBand band, string expectedText)
        => Assert.Contains(expectedText, CustomerMetrics.SuggestedAction(band));
}

public class ChurnEdgeCaseTests
{
    [Fact]
    public void Purchases_at_the_same_instant_do_not_cause_a_divide_by_zero()
    {
        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var tx = Enumerable.Range(0, 3).Select(_ => new PaidTransaction(now, 1000m)).ToList();

        var result = ChurnScorer.Score(tx, now);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Score);
    }
}
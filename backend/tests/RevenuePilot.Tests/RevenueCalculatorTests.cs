using System.Globalization;
using RevenuePilot.Domain.Analytics;
using RevenuePilot.Domain.Entities;
using Xunit;

namespace RevenuePilot.Tests;

public class RevenueCalculatorTests
{
    private static DateTime Utc(string iso) =>
        DateTime.SpecifyKind(DateTime.Parse(iso, CultureInfo.InvariantCulture), DateTimeKind.Utc);

    private static readonly Period October = new(Utc("2026-10-01T00:00:00"), Utc("2026-11-01T00:00:00"));

    private static OrderRecord Order(string iso, decimal total, OrderStatus status = OrderStatus.Paid, decimal refund = 0m) =>
        new(Guid.NewGuid(), Utc(iso), status, total, refund);

    [Fact]
    public void Net_revenue_counts_only_paid_orders_and_subtracts_refunds()
    {
        var orders = new[]
        {
            Order("2026-10-05T10:00:00", 1000m),
            Order("2026-10-06T10:00:00", 500m, refund: 100m),
            Order("2026-10-07T10:00:00", 700m, OrderStatus.Failed),
            Order("2026-10-08T10:00:00", 300m, OrderStatus.Cancelled),
            Order("2026-10-09T10:00:00", 900m, OrderStatus.Refunded),
        };
        Assert.Equal(1400m, RevenueCalculator.NetRevenue(orders, October));
        Assert.Equal(2, RevenueCalculator.OrderCount(orders, October));
        Assert.Equal(1, RevenueCalculator.FailedPaymentCount(orders, October));
    }

    [Fact]
    public void Period_start_is_inclusive_and_end_is_exclusive()
    {
        var orders = new[]
        {
            Order("2026-10-01T00:00:00", 100m),  // exactly at start: counted
            Order("2026-11-01T00:00:00", 999m),  // exactly at end: not counted
        };
        Assert.Equal(100m, RevenueCalculator.NetRevenue(orders, October));
    }

    [Fact]
    public void Growth_is_not_applicable_when_previous_period_is_zero()
        => Assert.Null(RevenueCalculator.GrowthPercent(500m, 0m));

    [Theory]
    [InlineData(125, 100, 25.0)]
    [InlineData(80, 100, -20.0)]
    [InlineData(100, 100, 0.0)]
    public void Growth_percentage(int current, int previous, double expected)
        => Assert.Equal((decimal)expected, RevenueCalculator.GrowthPercent(current, previous));

    [Fact]
    public void Average_order_value_is_null_when_there_are_no_orders()
        => Assert.Null(RevenueCalculator.AverageOrderValue(Array.Empty<OrderRecord>(), October));

    [Fact]
    public void Average_order_value_uses_net_revenue_per_qualifying_order()
    {
        var orders = new[]
        {
            Order("2026-10-05T10:00:00", 1000m),
            Order("2026-10-06T10:00:00", 500m, refund: 100m),
        };
        Assert.Equal(700m, RevenueCalculator.AverageOrderValue(orders, October)); // (1000 + 400) / 2
    }

    [Fact]
    public void Previous_period_is_the_same_length_immediately_before()
    {
        var prev = October.Previous();
        Assert.Equal(October.StartUtc, prev.EndUtc);
        Assert.Equal(October.Length, prev.Length);
    }

    [Fact]
    public void Last_days_uses_the_business_timezone_for_the_day_boundary()
    {
        // 23:30 UTC on 28 Oct is 01:30 on 29 Oct in Johannesburg (UTC+2), so "today" is the 29th.
        var p = PeriodCalculator.LastDays(Utc("2026-10-28T23:30:00"), "Africa/Johannesburg", 30);
        Assert.Equal(Utc("2026-10-29T22:00:00"), p.EndUtc);   // midnight at the end of the 29th, local
    }

    [Fact]
    public void Last_days_starts_the_right_number_of_days_back()
    {
        var p = PeriodCalculator.LastDays(Utc("2026-10-28T23:30:00"), "Africa/Johannesburg", 7);
        Assert.Equal(Utc("2026-10-22T22:00:00"), p.StartUtc); // 23 Oct 00:00 local
        Assert.Equal(TimeSpan.FromDays(7), p.Length);
    }
}
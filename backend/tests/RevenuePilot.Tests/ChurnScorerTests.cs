using RevenuePilot.Domain.Churn;
using Xunit;

namespace RevenuePilot.Tests;

public class ChurnScorerTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);

    // Builds n transactions every `gap` days; the most recent is `lastAgo` days before Now.
    private static List<PaidTransaction> Series(int n, int gap, int lastAgo, Func<int, decimal> amount) =>
        Enumerable.Range(0, n).Select(i => new PaidTransaction(Now.AddDays(-lastAgo - gap * i), amount(i))).ToList();

    [Fact]
    public void Returns_null_when_data_is_insufficient()
        => Assert.Null(ChurnScorer.Score(Series(2, 30, 5, _ => 1000m), Now));

    [Fact]
    public void Regular_customer_on_schedule_is_low_risk_with_no_evidence()
    {
        var r = ChurnScorer.Score(Series(7, 30, 5, _ => 1000m), Now)!;
        Assert.Equal(0, r.Score);
        Assert.Equal(RiskBand.Low, r.Band);
        Assert.Empty(r.Evidence);
    }

    [Fact]
    public void Long_overdue_customer_is_high_risk()
    {
        var r = ChurnScorer.Score(Series(7, 30, 120, _ => 1000m), Now)!;
        Assert.Equal(70, r.Score); // recency 60 (capped) + frequency 10
        Assert.Equal(RiskBand.High, r.Band);
        Assert.Contains(r.Evidence, e => e.Contains("days since last purchase"));
    }

    [Fact]
    public void Spend_decline_adds_points_and_evidence()
    {
        // newest three orders R500, older orders R1000
        var r = ChurnScorer.Score(Series(7, 30, 5, i => i < 3 ? 500m : 1000m), Now)!;
        Assert.Equal(30, r.Score);
        Assert.Equal(RiskBand.Low, r.Band);
        Assert.Contains(r.Evidence, e => e.Contains("below earlier average"));
    }

    [Fact]
    public void Thresholds_are_configurable()
    {
        var tx = Series(7, 30, 5, i => i < 3 ? 500m : 1000m); // score 30
        var r = ChurnScorer.Score(tx, Now, new ChurnOptions { MediumThreshold = 30 })!;
        Assert.Equal(RiskBand.Medium, r.Band);
    }

    [Theory]
    [InlineData(0, RiskBand.Low)]
    [InlineData(34, RiskBand.Low)]
    [InlineData(35, RiskBand.Medium)]
    [InlineData(59, RiskBand.Medium)]
    [InlineData(60, RiskBand.High)]
    [InlineData(100, RiskBand.High)]
    public void Band_boundaries(int score, RiskBand expected)
        => Assert.Equal(expected, ChurnScorer.BandFor(score, new ChurnOptions()));

    [Fact]
    public void Exposure_is_an_estimate_based_on_historical_pace()
    {
        var r = ChurnScorer.Score(Series(7, 30, 5, _ => 1000m), Now)!;
        Assert.Equal(3000m, r.EstimatedExposure); // R1000 x 1 order/month x 3 months
    }
}

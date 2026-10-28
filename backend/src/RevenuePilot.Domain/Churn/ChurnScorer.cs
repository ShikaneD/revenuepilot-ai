namespace RevenuePilot.Domain.Churn;

public enum RiskBand { Low, Medium, High }

public sealed record PaidTransaction(DateTime DateUtc, decimal Amount);

public sealed record ChurnOptions
{
    public int MediumThreshold { get; init; } = 35;
    public int HighThreshold { get; init; } = 60;
    public int MinTransactions { get; init; } = 3;
    public int ExposureMonths { get; init; } = 3;
}

public sealed record ChurnResult(
    int Score,
    RiskBand Band,
    IReadOnlyList<string> Evidence,
    decimal EstimatedExposure,
    int AverageGapDays,
    int DaysSinceLastPurchase);

/// <summary>
/// Explainable, rule-based risk score (0-100). It flags changes in behaviour;
/// it is NOT a statistically validated prediction. Pass PAID transactions only
/// (exclude refunds and cancellations before calling).
/// Components: recency (max 60) + spend decline (max 30) + frequency decline (max 10).
/// </summary>
public static class ChurnScorer
{
    public static ChurnResult? Score(IEnumerable<PaidTransaction> transactions, DateTime nowUtc, ChurnOptions? options = null)
    {
        var o = options ?? new ChurnOptions();
        var tx = transactions.OrderBy(t => t.DateUtc).ToList();
        if (tx.Count < o.MinTransactions) return null; // insufficient data: no score

        var evidence = new List<string>();

        // 1. Recency: time since last purchase relative to the customer's usual gap.
        var gaps = tx.Zip(tx.Skip(1), (a, b) => (b.DateUtc - a.DateUtc).TotalDays).ToList();
        var avgGap = Math.Max(1.0, gaps.Average());
        var since = (nowUtc - tx[^1].DateUtc).TotalDays;
        var recency = Math.Clamp((since / avgGap - 1) * 30, 0, 60);
        if (recency > 0)
            evidence.Add($"{Math.Round(since)} days since last purchase vs a usual gap of {Math.Round(avgGap)} days");

        // 2. Spend decline: recent average order vs earlier average order.
        var k = Math.Min(3, tx.Count / 2);
        var recent = tx.TakeLast(k).Average(t => t.Amount);
        var earlier = tx.Take(tx.Count - k).Average(t => t.Amount);
        var decline = earlier > 0 ? Math.Max(0.0, 1 - (double)(recent / earlier)) : 0.0;
        var spend = Math.Min(30, decline * 60);
        if (spend > 3)
            evidence.Add($"Recent average order R{recent:N0} is {Math.Round(decline * 100)}% below earlier average R{earlier:N0}");

        // 3. Frequency decline: last 90 days vs the 90 days before.
        var n90 = tx.Count(t => t.DateUtc > nowUtc.AddDays(-90));
        var n90Prev = tx.Count(t => t.DateUtc > nowUtc.AddDays(-180) && t.DateUtc <= nowUtc.AddDays(-90));
        var freqDecline = n90Prev > 0 ? Math.Max(0.0, 1 - (double)n90 / n90Prev) : 0.0;
        var freq = Math.Min(10, freqDecline * 20);
        if (freq > 2)
            evidence.Add($"{n90} purchases in the last 90 days vs {n90Prev} in the 90 days before");

        var score = (int)Math.Round(Math.Min(100, recency + spend + freq), MidpointRounding.AwayFromZero);

        // Estimate only: assumes the customer keeps buying at their historical pace.
        var avgAmount = tx.Average(t => t.Amount);
        var exposure = Math.Round(avgAmount * (decimal)(30.0 / avgGap) * o.ExposureMonths, 0);

        return new ChurnResult(score, BandFor(score, o), evidence, exposure, (int)Math.Round(avgGap), (int)Math.Round(since));
    }

    public static RiskBand BandFor(int score, ChurnOptions o) =>
        score >= o.HighThreshold ? RiskBand.High :
        score >= o.MediumThreshold ? RiskBand.Medium : RiskBand.Low;
}

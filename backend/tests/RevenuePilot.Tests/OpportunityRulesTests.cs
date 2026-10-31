using RevenuePilot.Domain.Entities;
using RevenuePilot.Domain.Opportunities;
using Xunit;

namespace RevenuePilot.Tests;

public class OpportunityRulesTests
{
    private static RuleResult Check(OpportunityStatus from, OpportunityStatus to, decimal? amount = null) =>
        OpportunityRules.ValidateTransition(from, to, amount);

    [Theory]
    [InlineData(OpportunityStatus.New, OpportunityStatus.InProgress)]
    [InlineData(OpportunityStatus.New, OpportunityStatus.Dismissed)]
    [InlineData(OpportunityStatus.InProgress, OpportunityStatus.Dismissed)]
    public void Open_opportunities_can_move_forward_without_an_amount(OpportunityStatus from, OpportunityStatus to)
        => Assert.True(Check(from, to).Ok);

    [Theory]
    [InlineData(OpportunityStatus.New)]
    [InlineData(OpportunityStatus.InProgress)]
    public void Recovering_with_a_valid_amount_is_allowed(OpportunityStatus from)
        => Assert.True(Check(from, OpportunityStatus.Recovered, 1500.50m).Ok);

    [Fact]
    public void Recovered_needs_an_amount()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.Recovered, null).Ok);

    [Fact]
    public void Recovered_rejects_a_zero_amount()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.Recovered, 0m).Ok);

    [Fact]
    public void Recovered_rejects_a_negative_amount()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.Recovered, -5m).Ok);

    [Fact]
    public void Recovered_rejects_more_than_two_decimal_places()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.Recovered, 10.001m).Ok);

    [Fact]
    public void Recovered_rejects_an_absurdly_large_amount()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.Recovered,
            OpportunityRules.MaxRecoveredValue + 1m).Ok);

    [Fact]
    public void A_recovered_amount_cannot_be_recorded_on_a_dismissal()
        => Assert.False(Check(OpportunityStatus.New, OpportunityStatus.Dismissed, 500m).Ok);

    [Fact]
    public void A_recovered_amount_cannot_be_recorded_while_in_progress()
        => Assert.False(Check(OpportunityStatus.New, OpportunityStatus.InProgress, 500m).Ok);

    [Fact]
    public void A_zero_amount_on_a_dismissal_is_harmless()
        => Assert.True(Check(OpportunityStatus.New, OpportunityStatus.Dismissed, 0m).Ok);

    [Theory]
    [InlineData(OpportunityStatus.Recovered)]
    [InlineData(OpportunityStatus.Dismissed)]
    public void Closed_opportunities_cannot_change(OpportunityStatus closed)
    {
        var result = Check(closed, OpportunityStatus.InProgress);
        Assert.False(result.Ok);
        Assert.Contains("closed", result.Error);
    }

    [Fact]
    public void An_opportunity_cannot_go_back_to_new()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.New).Ok);

    [Fact]
    public void Moving_to_the_same_status_is_rejected()
        => Assert.False(Check(OpportunityStatus.InProgress, OpportunityStatus.InProgress).Ok);

    [Theory]
    [InlineData(OpportunityStatus.New, true)]
    [InlineData(OpportunityStatus.InProgress, true)]
    [InlineData(OpportunityStatus.Recovered, false)]
    [InlineData(OpportunityStatus.Dismissed, false)]
    public void Only_new_and_in_progress_count_as_open(OpportunityStatus status, bool expected)
        => Assert.Equal(expected, OpportunityRules.IsOpen(status));
}